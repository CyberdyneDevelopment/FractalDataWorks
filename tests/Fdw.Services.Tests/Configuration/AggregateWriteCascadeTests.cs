using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

using TestRootConfiguration = Fdw.Services.Tests.Configuration.RecursiveCascadeSaveTests.TestRootConfiguration;
using TestRootCommand = Fdw.Services.Tests.Configuration.RecursiveCascadeSaveTests.TestRootCommand;
using TestBodyConfiguration = Fdw.Services.Tests.Configuration.RecursiveCascadeSaveTests.TestBodyConfiguration;
using TestBodyCommand = Fdw.Services.Tests.Configuration.RecursiveCascadeSaveTests.TestBodyCommand;
using TestOpConfiguration = Fdw.Services.Tests.Configuration.RecursiveCascadeSaveTests.TestOpConfiguration;
using TestMapConfiguration = Fdw.Services.Tests.Configuration.RecursiveCascadeSaveTests.TestMapConfiguration;
using Fdw.Services.Data;
using Moq;

namespace Fdw.Services.Tests.Configuration;

/// <summary>
/// Pins the write/delete-cascade behaviour changed on <c>feature/child-provider-write</c>: the cascade now
/// runs on EVERY save (not just the first), every header write is the SAME version-on-write
/// <c>ConfigurationSaveCommand</c> shape, an incomplete polymorphic aggregate fails loud with nothing
/// written, a header with no registered typed provider still saves as a leaf, and delete retires the whole
/// aggregate in REVERSE order (deepest child, then typed body, then header) keyed by the row's OWN id.
/// </summary>
/// <remarks>
/// Why a new file rather than adding to RecursiveCascadeSaveTests: that file pins the READ-side recursive
/// compose + the happy-path save shape. This file pins the WRITE-PATH MECHANISM itself (repeat-save
/// cascade, single write shape, the completeness gate, and the whole delete cascade) — a distinct set of
/// regressions the child-provider-write branch fixed, each of which must fail against the OLD behaviour.
/// </remarks>
[Collection(nameof(ServicesTestCollection))]
public sealed class AggregateWriteCascadeTests
{
    private static ImplementationConfigurationProviderBase<TestRootConfiguration, TestRootCommand> MakeProvider(RecordingGateway gateway)
        => new(
            NullLogger<ImplementationConfigurationProviderBase<TestRootConfiguration, TestRootCommand>>.Instance,
            GatewayProviderFor(gateway),
            "ConfigurationDb",
            "pipe");

    // ========================================================================
    // 1. Cascade runs on a repeat save (the update case)
    // ========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public async Task SaveCascadesTypedBodyAndChildrenOnRepeatSaveNotJustFirst()
    {
        var mapping = new TestMapConfiguration { Id = Guid.NewGuid(), Name = "Map" };
        var operation = new TestOpConfiguration { Id = Guid.NewGuid(), Name = "Op", Mappings = { mapping } };
        var body = new TestBodyConfiguration { Id = Guid.NewGuid(), Name = "Body", Operations = { operation } };
        var root = new TestRootConfiguration { Id = Guid.NewGuid(), Name = "Root", Configuration = body };

        var gateway = new RecordingGateway();
        var provider = MakeProvider(gateway);

        var first = await provider.Save(root, TestContext.Current.CancellationToken);
        var second = await provider.Save(root, TestContext.Current.CancellationToken);

        first.IsSuccess.ShouldBeTrue();
        second.IsSuccess.ShouldBeTrue();

        // Two full cascades means TWO rows per level — one per Save call — not one.
        gateway.SavedConfigs.OfType<TestBodyConfiguration>().Count().ShouldBe(2);
        gateway.SavedConfigs.OfType<TestOpConfiguration>().Count().ShouldBe(2);
        gateway.SavedConfigs.OfType<TestMapConfiguration>().Count().ShouldBe(2);
    }

    // ========================================================================
    // 2. There is only one write shape
    // ========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public async Task SaveAlwaysUsesVersionOnWriteSaveCommandForHeaderNeverAPlainUpdate()
    {
        var root = new TestRootConfiguration { Id = Guid.NewGuid(), Name = "Root" };

        var gateway = new RecordingGateway();
        var provider = MakeProvider(gateway);

        await provider.Save(root, TestContext.Current.CancellationToken);
        await provider.Save(root, TestContext.Current.CancellationToken);

        var headerCommands = gateway.AllCommands
            .Where(c => c.Target.Container == "TestRoot")
            .Select(c => c.Command)
            .ToList();

        headerCommands.Count.ShouldBe(2);
        headerCommands.ShouldAllBe(c => c is ConfigurationSaveCommand<TestRootConfiguration>);
    }

    // ========================================================================
    // 3. Fail loud on an incomplete aggregate
    // ========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public async Task SaveFailsLoudAndWritesNothingWhenTypedBodyMissingForRegisteredDiscriminator()
    {
        var gateway = new RecordingGateway();
        var provider = MakeProvider(gateway);
        provider.Register(
            "Default",
            new ImplementationConfigurationProviderBase<TestBodyConfiguration, TestBodyCommand>(
                NullLogger<ImplementationConfigurationProviderBase<TestBodyConfiguration, TestBodyCommand>>.Instance,
                GatewayProviderFor(gateway),
                "ConfigurationDb",
                "pipe"));

        // ServiceOptionType is fixed to "Default" on TestRootConfiguration — a provider IS registered for
        // it above — yet Configuration (the typed body) is left null.
        var header = new TestRootConfiguration { Id = Guid.NewGuid(), Name = "Root" };

        var result = await provider.Save(header, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        gateway.AllCommands.ShouldBeEmpty();
        gateway.SavedConfigs.ShouldBeEmpty();
    }

    // ========================================================================
    // 4. A header with no registered typed provider still saves
    // ========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public async Task SaveSucceedsWhenNoTypedProviderIsRegisteredForTheDiscriminator()
    {
        var gateway = new RecordingGateway();
        var provider = MakeProvider(gateway); // No Register call — registry stays empty.

        var header = new TestRootConfiguration { Id = Guid.NewGuid(), Name = "Root" };

        var result = await provider.Save(header, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        gateway.SavedConfigs.OfType<TestRootConfiguration>().ShouldHaveSingleItem();
    }

    // ========================================================================
    // 5. Delete cascades in REVERSE order
    // ========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public async Task DeleteCascadesReverseOrderDeepestChildThenTypedBodyThenHeader()
    {
        var operation = new TestOpConfiguration { Id = Guid.NewGuid(), Name = "Op" };
        var body = new TestBodyConfiguration { Id = Guid.NewGuid(), Name = "Body", Operations = { operation } };
        var root = new TestRootConfiguration { Id = Guid.NewGuid(), Name = "Root", Configuration = body };

        var gateway = new RecordingGateway { RootHeader = root };
        var provider = MakeProvider(gateway);

        var result = await provider.Delete(root.Id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();

        var deletes = gateway.AllCommands
            .Where(c => c.Command is ConfigurationDeleteCommand)
            .Select(c => (Container: c.Target.Container, Id: ((ConfigurationDeleteCommand)c.Command).Data))
            .ToList();

        deletes.Count.ShouldBe(3);
        deletes[0].Container.ShouldBe("TestOp");
        deletes[0].Id.ShouldBe(operation.Id);
        deletes[1].Container.ShouldBe("TestBody");
        deletes[1].Id.ShouldBe(body.Id);
        deletes[2].Container.ShouldBe("TestRoot");
        deletes[2].Id.ShouldBe(root.Id);
    }

    // ========================================================================
    // 6. Delete fails loud, writing no delete command, for every "nothing to delete" case
    // ========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public async Task DeleteFailsLoudForGuidEmptyAndWritesNoCommand()
    {
        var gateway = new RecordingGateway();
        var provider = MakeProvider(gateway);

        var result = await provider.Delete(Guid.Empty, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        gateway.AllCommands.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public async Task DeleteFailsLoudForNonExistentIdAndWritesNoCommand()
    {
        var gateway = new RecordingGateway(); // RootHeader stays null -> "not found".
        var provider = MakeProvider(gateway);

        var result = await provider.Delete(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        gateway.AllCommands.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public async Task DeleteFailsLoudForEmptyNameAndWritesNoCommand()
    {
        var gateway = new RecordingGateway();
        var provider = MakeProvider(gateway);

        var result = await provider.Delete(string.Empty, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        gateway.AllCommands.ShouldBeEmpty();
    }

    // ========================================================================
    // 7. Delete retires by the record's OWN durable Id, not the argument used to find it
    // ========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public async Task DeleteRetiresByRowsOwnIdNotTheArgumentPassedToFindIt()
    {
        var domainConfigurationId = Guid.NewGuid();
        var body = new TestBodyConfiguration { Id = Guid.NewGuid(), Name = "Body" };

        // The gateway always answers a TestBodyConfiguration header read with `body`, regardless of the
        // id used to look it up — simulating a parent-join read (caller passes the PARENT's id) that
        // resolves to a row with its own distinct durable Id.
        var gateway = new RecordingGateway { BodyHeader = body };
        var bodyProvider = new ImplementationConfigurationProviderBase<TestBodyConfiguration, TestBodyCommand>(
            NullLogger<ImplementationConfigurationProviderBase<TestBodyConfiguration, TestBodyCommand>>.Instance,
            GatewayProviderFor(gateway),
            "ConfigurationDb",
            "pipe");

        var result = await bodyProvider.Delete(domainConfigurationId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();

        var headerDelete = (ConfigurationDeleteCommand)gateway.AllCommands
            .Single(c => c.Command is ConfigurationDeleteCommand).Command;
        headerDelete.Data.ShouldBe(body.Id);
        headerDelete.Data.ShouldNotBe(domainConfigurationId);
    }

    // ========================================================================
    // Test infrastructure
    // ========================================================================

    /// <summary>
    /// Gateway test double that records every write command (save or delete, generic or non-generic) the
    /// provider issues, IN CALL ORDER, and answers header reads from settable fields. Query reads never
    /// land in <see cref="AllCommands"/> — only saves/deletes count toward the cascade order/shape
    /// assertions these tests make.
    /// </summary>
    private sealed class RecordingGateway : IConfigurationGateway
    {
        /// <summary>The connection this fake stands in for.</summary>
        public string ConnectionName => "ConfigurationDb";

        /// <summary>Targets this fake was asked to invalidate, in call order.</summary>
        public List<DataStoreTarget> Invalidated { get; } = [];

        public void InvalidateCachedResults(DataStoreTarget target) => Invalidated.Add(target);

        /// <summary>Every write command the gateway received, in call order.</summary>
        public List<(IDataCommand Command, DataStoreTarget Target)> AllCommands { get; } = [];

        /// <summary>Every configuration POCO saved through a ConfigurationSaveCommand, in call order.</summary>
        public List<object> SavedConfigs { get; } = [];

        /// <summary>Root header row returned by any TestRootConfiguration header read. Null = "not found".</summary>
        public TestRootConfiguration? RootHeader { get; set; }

        /// <summary>
        /// Body row returned by any TestBodyConfiguration header read — used to simulate a typed-body
        /// provider's Get(Guid), which resolves by the PARENT's id yet returns a row with its OWN distinct Id.
        /// </summary>
        public TestBodyConfiguration? BodyHeader { get; set; }

        public IReadOnlyList<IDataStore> DataStores { get; } = [];

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, CancellationToken cancellationToken = default)
            => Execute<T>(command, default(DataStoreTarget)!, cancellationToken);

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken cancellationToken = default)
            => Execute<T>(command, target, cancellationToken);

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        {
            // A header READ asks for IEnumerable<TConfig> — answer from the configured header fields.
            // This is a QUERY, never recorded as a write.
            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var elementType = typeof(T).GetGenericArguments()[0];
                if (elementType == typeof(TestRootConfiguration))
                    return Task.FromResult(GenericResult<T>.Success((T)(object)(RootHeader is null
                        ? new List<TestRootConfiguration>()
                        : new List<TestRootConfiguration> { RootHeader })));
                if (elementType == typeof(TestBodyConfiguration))
                    return Task.FromResult(GenericResult<T>.Success((T)(object)(BodyHeader is null
                        ? new List<TestBodyConfiguration>()
                        : new List<TestBodyConfiguration> { BodyHeader })));
                return Task.FromResult(GenericResult<T>.Success((T)(object)Array.CreateInstance(elementType, 0)));
            }

            // Anything else is a header SAVE or a header DELETE — record it in call order.
            AllCommands.Add((command, target));
            if (command is IConfigurationSaveCommand save && save.InputData is not null)
                SavedConfigs.Add(save.InputData);
            return Task.FromResult(GenericResult<T>.Success(default!));
        }

        public Task<IGenericResult> Execute(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        {
            // Every CHILD save/delete routes through the non-generic Execute — record it in the SAME
            // list as the header writes so cross-level ordering is observable from one sequence.
            AllCommands.Add((command, target));
            if (command is IConfigurationSaveCommand save && save.InputData is not null)
                SavedConfigs.Add(save.InputData);
            return Task.FromResult<IGenericResult>(GenericResult.Success());
        }

        public Task<IGenericResult<IEnumerable<object>>> Execute(IDataCommand command, DataStoreTarget target, Type rowType, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<IEnumerable<object>>.Success(Array.Empty<object>()));

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataSetTarget target, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<T>.Failure(new GenericMessage("DataSet routing not supported in RecordingGateway test double")));

        public Task<IGenericResult<Fdw.Data.RowSources.Abstractions.IRecordSource<Fdw.Data.RowSources.Abstractions.DataRecord>>> OpenRecordSource(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IGenericResult<IDataGatewayTransaction>> BeginTransaction(string connectionName, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<IDataGatewayTransaction>.Failure(new GenericMessage("Transactions not supported in test double")));
    }

    private static IConfigurationGatewayProvider GatewayProviderFor(IConfigurationGateway gateway)
        => new AnyConnectionGateways(gateway);

    private sealed class AnyConnectionGateways : IConfigurationGatewayProvider
    {
        private readonly IConfigurationGateway _gateway;

        public AnyConnectionGateways(IConfigurationGateway gateway) => _gateway = gateway;

        public IGenericResult<IConfigurationGateway> Get(string connectionName)
            => GenericResult<IConfigurationGateway>.Success(_gateway);

        public IGenericResult Register(IConfigurationGateway gateway) => GenericResult.Success();
    }

}
