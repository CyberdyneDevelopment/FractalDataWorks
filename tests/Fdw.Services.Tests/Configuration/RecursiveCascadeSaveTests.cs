using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Configuration;

/// <summary>
/// Verifies the N-level recursive configuration cascade-save: a root record whose typed body
/// (<c>Configuration</c> property) carries a child collection, where each child carries its own
/// child collection, persists ALL levels with the correct logical foreign keys set.
/// </summary>
/// <remarks>
/// Why: the cascade was single-level (root collections only) and never recursed into the typed
/// body's collections or their grandchildren — so pipeline operations and their field mappings were
/// silently dropped on save. This test pins the generalized recursive behavior (root -> typed-body
/// operations -> field mappings) and the FK derivation at each level.
/// </remarks>
[Collection(nameof(ServicesTestCollection))]
public sealed class RecursiveCascadeSaveTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public async Task SavePersistsTypedBodyOperationsAndFieldMappingsWithForeignKeys()
    {
        // Arrange — a root with a typed body holding one operation that holds one field mapping.
        var rootId = Guid.NewGuid();
        var mapping = new TestMapConfiguration { Id = Guid.NewGuid(), Name = "Map" };
        var operation = new TestOpConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Op",
            Mappings = { mapping }
        };
        var body = new TestBodyConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Body",
            Operations = { operation }
        };
        var root = new TestRootConfiguration
        {
            Id = rootId,
            Name = "Root",
            Configuration = body
        };

        var gateway = new RecordingGateway();
        var provider = MakeProvider(gateway);

        // Act
        var result = await provider.Save(root, TestContext.Current.CancellationToken);

        // Assert — save succeeded and every level was persisted via its own ConfigurationSaveCommand.
        result.IsSuccess.ShouldBeTrue();

        gateway.SavedConfigs.OfType<TestBodyConfiguration>().ShouldHaveSingleItem();
        gateway.SavedConfigs.OfType<TestOpConfiguration>().ShouldHaveSingleItem();
        gateway.SavedConfigs.OfType<TestMapConfiguration>().ShouldHaveSingleItem();

        // The typed body is FK'd to the ROOT record's logical Id (Strip(TestRootConfiguration)+"Id").
        body.TestRootId.ShouldBe(rootId);

        // Level-1 child (operation, on the typed body) is FK'd to its IMMEDIATE owner — the typed body —
        // NOT the root (Strip(TestBodyConfiguration)+"Id" = TestBodyId). This is the corrected cascade:
        // typed-body collections key to the body that owns them, matching the real DDL (e.g.
        // pipe.PipelineOperation.EtlPipelineId, conn.MsSqlConnectionLimit.MsSqlConnectionId).
        operation.TestBodyId.ShouldBe(body.Id);

        // Level-2 child (field mapping) is FK'd to its parent operation's logical Id.
        mapping.TestOpId.ShouldBe(operation.Id);
    }

    // Why: the read mirror of the no-fallback guarantee — a domain record naming a ServiceOptionType
    // for which no implementation configuration provider is registered must FAIL LOUD, never silently
    // return the bare record. This is the pipeline case "Get a pipeline whose kind has no provider".
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public async Task GetFailsLoudWhenKindHasNoRegisteredTypedProvider()
    {
        // Arrange — header row carries ServiceOptionType "Default" (TestRoot's discriminator).
        var header = new TestRootConfiguration { Id = Guid.NewGuid(), Name = "Root" };
        var gateway = new HeaderReturningGateway(header);
        var provider = new ImplementationConfigurationProviderBase<TestRootConfiguration, TestRootCommand>(
            NullLogger<ImplementationConfigurationProviderBase<TestRootConfiguration, TestRootCommand>>.Instance,
            new Lazy<IConfigurationGateway>(() => gateway),
            "ConfigurationDb",
            "pipe");

        // Register a typed provider for a DIFFERENT kind: the registry is NON-EMPTY (so this is a header
        // provider, not a leaf) yet cannot resolve "Default" — the missing-provider condition.
        provider.Register(
            "SomeOtherKind",
            new ImplementationConfigurationProviderBase<TestBodyConfiguration, TestBodyCommand>(
                NullLogger<ImplementationConfigurationProviderBase<TestBodyConfiguration, TestBodyCommand>>.Instance,
                new Lazy<IConfigurationGateway>(() => gateway),
                "ConfigurationDb",
                "pipe"));

        // Act — Get by name reads the header then composes the typed body; "Default" has no provider.
        var result = await provider.Get("Root", TestContext.Current.CancellationToken);

        // Assert — fail loud, no silent fallback to the bare header. NO FALLBACKS WITHOUT EXPLICIT APPROVAL.
        result.IsSuccess.ShouldBeFalse();
    }

    private static ImplementationConfigurationProviderBase<TestRootConfiguration, TestRootCommand> MakeProvider(RecordingGateway gateway)
    {

        return new ImplementationConfigurationProviderBase<TestRootConfiguration, TestRootCommand>(
            NullLogger<ImplementationConfigurationProviderBase<TestRootConfiguration, TestRootCommand>>.Instance,
            new Lazy<IConfigurationGateway>(() => gateway),
            "ConfigurationDb",
            "pipe");
    }

    // ========================================================================
    // Test infrastructure: a 3-level configuration hierarchy
    // ========================================================================

    /// <summary>
    /// Marker interface for the test's typed body. Why: the generated mapper detects a typed-body
    /// "Configuration" property only when its type is a config interface that *derives from*
    /// IGenericConfiguration (the production pattern — IConnectionImplementationConfiguration, ISecretManagerImplementationConfiguration).
    /// A property typed as the bare IGenericConfiguration is treated as a scalar, so a derived interface
    /// is required for GetTypedBody to return the body.
    /// </summary>
    public interface ITestBodyConfiguration : IGenericConfiguration
    {
    }

    /// <summary>Root record carrying a typed body in its <c>Configuration</c> property.</summary>
    // Why: the reflection-free cascade discovers the typed body + child collections via this type's
    // generated PocoMapper (GetTypedBody / CascadeChildren / SetValue), exactly as real config types do.
    [GenerateMapper]
    public sealed class TestRootConfiguration : IGenericConfiguration
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string SectionName => "TestRoot";
        public string ServiceType => "TestRoot";
        public string? ServiceOptionType => "Default";

        /// <summary>The typed body whose own child collections must also cascade.</summary>
        public ITestBodyConfiguration? Configuration { get; set; }
    }

    /// <summary>Typed body holding a child collection (operations).</summary>
    [GenerateMapper]
    public sealed class TestBodyConfiguration : ITestBodyConfiguration
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string SectionName => "TestBody";
        public string ServiceType => "TestRoot";
        public string? ServiceOptionType => "Default";

        /// <summary>FK to the root, set by the cascade (Strip(TestRootConfiguration)+"Id").</summary>
        public Guid TestRootId { get; set; }

        // Why: settable collection — the generated mapper only treats a child collection as a cascade
        // child when it has a public setter (matches every real config type, e.g. DataSetConfiguration.Fields).
        public IList<TestOpConfiguration> Operations { get; set; } = [];
    }

    /// <summary>Operation holding its own child collection (mappings).</summary>
    [GenerateMapper]
    public sealed class TestOpConfiguration : IGenericConfiguration
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string SectionName => "TestOp";
        public string ServiceType => "TestRoot";
        public string? ServiceOptionType => "Default";

        /// <summary>FK to the typed body (immediate owner), set by the cascade (Strip(TestBodyConfiguration)+"Id").</summary>
        public Guid TestBodyId { get; set; }

        public IList<TestMapConfiguration> Mappings { get; set; } = [];
    }

    /// <summary>Leaf field mapping.</summary>
    [GenerateMapper]
    public sealed class TestMapConfiguration : IGenericConfiguration
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string SectionName => "TestMap";
        public string ServiceType => "TestRoot";
        public string? ServiceOptionType => "Default";

        /// <summary>FK to the parent operation, set by the cascade (Strip(TestOpConfiguration)+"Id").</summary>
        public Guid TestOpId { get; set; }
    }

    // Why: ConfigurationCommands TypeCollection is auto-populated only for referenced assemblies.
    // These test-assembly commands are registered manually in ServicesTypeCollectionFixture.
    // The TypeOption key is the STRIPPED config name (the production convention, e.g.
    // [TypeOption(..., "CalculationEntity")]) — SaveOneChild resolves the command via
    // ConfigurationCommands.ByName(StripConfigurationSuffix(childType.Name)).
    [TypeOption(typeof(ConfigurationCommands), "TestRoot")]
    public sealed class TestRootCommand : ConfigurationCommandBase<TestRootConfiguration>
    {
        public TestRootCommand() : base("TestRoot") { }
    }

    [TypeOption(typeof(ConfigurationCommands), "TestBody")]
    public sealed class TestBodyCommand : ConfigurationCommandBase<TestBodyConfiguration>
    {
        public TestBodyCommand() : base("TestBody") { }
    }

    [TypeOption(typeof(ConfigurationCommands), "TestOp")]
    public sealed class TestOpCommand : ConfigurationCommandBase<TestOpConfiguration>
    {
        public TestOpCommand() : base("TestOp") { }
    }

    [TypeOption(typeof(ConfigurationCommands), "TestMap")]
    public sealed class TestMapCommand : ConfigurationCommandBase<TestMapConfiguration>
    {
        public TestMapCommand() : base("TestMap") { }
    }

    /// <summary>
    /// Gateway test double that records every configuration record saved through it so the test can
    /// assert which levels of the hierarchy were persisted. The root's existence-check read
    /// (<c>Execute&lt;IEnumerable&lt;T&gt;&gt;</c>) returns empty so Save treats the record as new.
    /// </summary>
    private sealed class RecordingGateway : IConfigurationGateway
    {
        /// <summary>Targets this fake was asked to invalidate, in call order.</summary>
        public List<DataStoreTarget> Invalidated { get; } = [];

        public void InvalidateCachedResults(DataStoreTarget target) => Invalidated.Add(target);

        public List<object> SavedConfigs { get; } = [];

        public IReadOnlyList<IDataStore> DataStores { get; } = [];

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, CancellationToken cancellationToken = default)
            => Execute<T>(command, default(DataStoreTarget)!, cancellationToken);

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken cancellationToken = default)
            // Why: test double — useCache not exercised in cascade-save tests; delegates to existing implementation.
            => Execute<T>(command, target, cancellationToken);

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        {
            // The Get(id) existence-check inside Save reads IEnumerable<T> — return empty (record is new).
            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return Task.FromResult(GenericResult<T>.Success((T)(object)Array.CreateInstance(typeof(T).GetGenericArguments()[0], 0)));

            // A save command exposes the saved POCO via IConfigurationSaveCommand.InputData.
            if (command is IConfigurationSaveCommand save && save.InputData is not null)
                SavedConfigs.Add(save.InputData);

            return Task.FromResult(GenericResult<T>.Success(default!));
        }

        public Task<IGenericResult> Execute(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        {
            // The save cascade routes child saves through the non-generic Execute — record them too.
            if (command is IConfigurationSaveCommand save && save.InputData is not null)
                SavedConfigs.Add(save.InputData);
            return Task.FromResult<IGenericResult>(GenericResult.Success());
        }

        // Why: by-type child read — the existence-check read returns empty, so child composition never
        // fires in these save tests; an empty result satisfies the interface.
        public Task<IGenericResult<IEnumerable<object>>> Execute(IDataCommand command, DataStoreTarget target, Type rowType, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<IEnumerable<object>>.Success(Array.Empty<object>()));

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataSetTarget target, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<T>.Failure(new GenericMessage("DataSet routing not supported in RecordingGateway test double")));

        // Why: streaming record-source cursor is not exercised by this test double.
        public Task<IGenericResult<Fdw.Data.RowSources.Abstractions.IRecordSource<Fdw.Data.RowSources.Abstractions.DataRecord>>> OpenRecordSource(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => throw new System.NotImplementedException();

        public Task<IGenericResult<IDataGatewayTransaction>> BeginTransaction(string connectionName, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<IDataGatewayTransaction>.Failure(new GenericMessage("Transactions not supported in test double")));
    }

    /// <summary>
    /// Gateway test double that returns a single supplied <see cref="TestRootConfiguration"/> on the
    /// by-name read (so Get(name) composes the implementation configuration), and empty for every other
    /// read. Exercises the fail-loud missing-provider path without a mock schema tree.
    /// </summary>
    private sealed class HeaderReturningGateway : IConfigurationGateway
    {
        /// <summary>Targets this fake was asked to invalidate, in call order.</summary>
        public List<DataStoreTarget> Invalidated { get; } = [];

        public void InvalidateCachedResults(DataStoreTarget target) => Invalidated.Add(target);

        private readonly TestRootConfiguration _header;

        public HeaderReturningGateway(TestRootConfiguration header) => _header = header;

        public IReadOnlyList<IDataStore> DataStores { get; } = [];

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, CancellationToken cancellationToken = default)
            => Execute<T>(command, default(DataStoreTarget)!, cancellationToken);

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken cancellationToken = default)
            // Why: test double — useCache not exercised in cascade-save tests; delegates to existing implementation.
            => Execute<T>(command, target, cancellationToken);

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        {
            // The Get(name) header read requests IEnumerable<TestRootConfiguration> — return the one header.
            if (typeof(T) == typeof(IEnumerable<TestRootConfiguration>))
                return Task.FromResult(GenericResult<T>.Success((T)(object)new List<TestRootConfiguration> { _header }));

            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return Task.FromResult(GenericResult<T>.Success((T)(object)Array.CreateInstance(typeof(T).GetGenericArguments()[0], 0)));

            return Task.FromResult(GenericResult<T>.Success(default!));
        }

        public Task<IGenericResult> Execute(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => Task.FromResult<IGenericResult>(GenericResult.Success());

        public Task<IGenericResult<IEnumerable<object>>> Execute(IDataCommand command, DataStoreTarget target, Type rowType, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<IEnumerable<object>>.Success(Array.Empty<object>()));

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataSetTarget target, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<T>.Failure(new GenericMessage("DataSet routing not supported in HeaderReturningGateway test double")));

        // Why: streaming record-source cursor is not exercised by this test double.
        public Task<IGenericResult<Fdw.Data.RowSources.Abstractions.IRecordSource<Fdw.Data.RowSources.Abstractions.DataRecord>>> OpenRecordSource(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => throw new System.NotImplementedException();

        public Task<IGenericResult<IDataGatewayTransaction>> BeginTransaction(string connectionName, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<IDataGatewayTransaction>.Failure(new GenericMessage("Transactions not supported in test double")));
    }
}
