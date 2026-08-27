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

// Why: the 3-level test hierarchy (TestRootConfiguration -> TestBodyConfiguration -> TestOpConfiguration
// -> TestMapConfiguration), its ConfigurationCommand TypeOptions, and its generated PocoMappers already
// exist as public nested types on RecursiveCascadeSaveTests and are already registered once, for the
// whole test collection, by ServicesTypeCollectionFixture. Aliasing them here reuses that exact fixture
// (no duplicate [TypeOption]/[GenerateMapper] registration) while reading like the unqualified names used
// in RecursiveCascadeSaveTests itself.
using TestRootConfiguration = Fdw.Services.Tests.Configuration.RecursiveCascadeSaveTests.TestRootConfiguration;
using TestRootCommand = Fdw.Services.Tests.Configuration.RecursiveCascadeSaveTests.TestRootCommand;
using TestBodyConfiguration = Fdw.Services.Tests.Configuration.RecursiveCascadeSaveTests.TestBodyConfiguration;
using TestBodyCommand = Fdw.Services.Tests.Configuration.RecursiveCascadeSaveTests.TestBodyCommand;
using TestOpConfiguration = Fdw.Services.Tests.Configuration.RecursiveCascadeSaveTests.TestOpConfiguration;
using TestMapConfiguration = Fdw.Services.Tests.Configuration.RecursiveCascadeSaveTests.TestMapConfiguration;

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
    private static DefaultConfigurationProvider<TestRootConfiguration, TestRootCommand> MakeProvider(RecordingGateway gateway)
        => new(
            NullLogger<DefaultConfigurationProvider<TestRootConfiguration, TestRootCommand>>.Instance,
            new Lazy<IConfigurationGateway>(() => gateway),
            "ConfigurationDb",
            "pipe");

    // ========================================================================
    // 1. Cascade runs on a repeat save (the update case)
    // ========================================================================

    // Why: the OLD cascade ran only when the record did not already exist (an implicit "is new" probe),
    // so the SECOND save of an already-persisted aggregate persisted the header alone — the typed body and
    // every collection child silently vanished from every update. Saving the SAME aggregate twice must
    // persist the full tree BOTH times.
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

    // Why: the OLD write path probed Get(record.Id) and branched to a plain in-place UpdateCommand
    // whenever the probe reported "exists" — a command with no version-on-write semantics that also
    // skipped the cascade entirely. The header command recorded for the header's own table must be the
    // SAME ConfigurationSaveCommand shape on every save, never a different command type.
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

    // Why: a polymorphic header IS its typed body — the two rows are ONE aggregate. A header whose
    // discriminator HAS a registered typed provider but whose typed-body property is null must fail
    // rather than persist half an aggregate, and NOTHING may reach the gateway.
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public async Task SaveFailsLoudAndWritesNothingWhenTypedBodyMissingForRegisteredDiscriminator()
    {
        var gateway = new RecordingGateway();
        var provider = MakeProvider(gateway);
        provider.Register(
            "Default",
            new DefaultConfigurationProvider<TestBodyConfiguration, TestBodyCommand>(
                NullLogger<DefaultConfigurationProvider<TestBodyConfiguration, TestBodyCommand>>.Instance,
                new Lazy<IConfigurationGateway>(() => gateway),
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

    // Why: the completeness gate must NOT catch every header carrying a discriminator — only ones for
    // which a typed provider is actually registered. A discriminator with no registered provider is the
    // leaf/nested-body case and must save successfully on the header alone.
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

    // Why: Save persists the owner row and THEN its children; Delete must retire the children and THEN
    // the owner, because every child is reached through the owner and retiring the owner first makes its
    // subtree unreachable, leaving it live at rest. The recorded delete order must be deepest child, then
    // typed body, then header — never the other way round.
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

    // Why: a typed-body provider resolves Get(Guid) by the PARENT's durable Id (see
    // DefaultConfigurationProvider.Get(Guid)) — the row it returns carries its OWN distinct Id. Passing
    // the caller's id straight through to the delete command targets [Id]=<the argument>, which matches
    // nothing on the child table and silently retires no row. The delete command must carry the resolved
    // row's own Id.
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
        var bodyProvider = new DefaultConfigurationProvider<TestBodyConfiguration, TestBodyCommand>(
            NullLogger<DefaultConfigurationProvider<TestBodyConfiguration, TestBodyCommand>>.Instance,
            new Lazy<IConfigurationGateway>(() => gateway),
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

        // Why: kept empty — every scenario in this file uses providers with no parent FK (ResolveParentJoin
        // degrades to the no-join sentinel) and root types with no typed-list CascadeChildren of their own,
        // so no schema-tree mock is needed to exercise the write/delete cascade mechanism under test.
        public IReadOnlyList<IDataStore> DataStores { get; } = [];

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, CancellationToken cancellationToken = default)
            => Execute<T>(command, default(DataStoreTarget)!, cancellationToken);

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken cancellationToken = default)
            // Why: test double — useCache not exercised by these cascade tests; delegates to the existing implementation.
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

        // Why: by-type child read — none of these tests exercise typed-list child composition on the
        // read side (every owner's own CascadeChildren resolution here bottoms out with an empty schema
        // tree), so an empty result satisfies the interface without building a mock schema tree.
        public Task<IGenericResult<IEnumerable<object>>> Execute(IDataCommand command, DataStoreTarget target, Type rowType, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<IEnumerable<object>>.Success(Array.Empty<object>()));

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataSetTarget target, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<T>.Failure(new GenericMessage("DataSet routing not supported in RecordingGateway test double")));

        // Why: streaming record-source cursor is not exercised by these tests.
        public Task<IGenericResult<Fdw.Data.RowSources.Abstractions.IRecordSource<Fdw.Data.RowSources.Abstractions.DataRecord>>> OpenRecordSource(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IGenericResult<IDataGatewayTransaction>> BeginTransaction(string connectionName, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<IDataGatewayTransaction>.Failure(new GenericMessage("Transactions not supported in test double")));
    }
}
