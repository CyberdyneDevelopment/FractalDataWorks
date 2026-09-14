using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Configuration;

/// <summary>
/// Verifies the N-level recursive child cascade a save performs: an implementation record whose
/// child collection's items carry their own child collections persists every level, with the
/// logical foreign key each level hangs from.
/// </summary>
/// <remarks>
/// Why: the cascade is what makes an aggregate one thing to write. Without it a pipeline's
/// operations and their field mappings are dropped silently on save, which is how they were lost
/// before. The foreign key at each level is its IMMEDIATE owner's -- Strip(owner type) + "Id" --
/// never the root's, matching the DDL (pipe.PipelineOperation.EtlPipelineId,
/// conn.MsSqlConnectionLimit.MsSqlConnectionId).
/// </remarks>
[Collection(nameof(ServicesTestCollection))]
public sealed class RecursiveCascadeSaveTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public async Task SavePersistsOperationsAndTheirFieldMappingsWithForeignKeys()
    {
        var mapping = new TestMapConfiguration { Id = Guid.NewGuid(), Name = "Map" };
        var operation = new TestOpConfiguration { Id = Guid.NewGuid(), Name = "Op", Mappings = { mapping } };
        var root = new TestRootConfiguration { Id = Guid.NewGuid(), Name = "Root", Operations = { operation } };

        var gateway = new RecordingGateway();

        var result = await ImplementationProvider(gateway).Save(root, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        gateway.SavedConfigs.OfType<TestRootConfiguration>().ShouldHaveSingleItem();
        gateway.SavedConfigs.OfType<TestOpConfiguration>().ShouldHaveSingleItem();
        gateway.SavedConfigs.OfType<TestMapConfiguration>().ShouldHaveSingleItem();

        operation.TestRootId.ShouldBe(root.Id);
        mapping.TestOpId.ShouldBe(operation.Id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public async Task GetFailsLoudWhenTheRowNamesAnImplementationNothingIsRegisteredFor()
    {
        var gateway = new HeaderReturningGateway(new DomainConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Root",
            Domain = "TestRoot",
            Implementation = "Default",
        });
        var domain = new TestRootDomainProvider(GatewayProviderFor(gateway));

        // Registered under a DIFFERENT name: the row names "Default", which nothing answers for.
        domain.Register("SomeOtherKind", ImplementationProvider(gateway));

        var result = await domain.Get("Root", TestContext.Current.CancellationToken);

        // Fail loud rather than hand back a domain row with no implementation behind it.
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public async Task DomainSaveStampsTheDomainIdOntoTheImplementation()
    {
        var gateway = new RecordingGateway();
        var domain = new TestRootDomainProvider(GatewayProviderFor(gateway));
        domain.Register("Default", ImplementationProvider(gateway));

        var result = await domain.Save(
            new TestRootConfiguration(), "TestRoot", "Default", "Root", TestContext.Current.CancellationToken);

        // The save translator resolves TestRootDomainRowId from TestRootDomainId. Stamping only Id
        // left it empty, and the database refused the implementation row with a NULL RowId.
        result.IsSuccess.ShouldBeTrue();
        var domainRow = gateway.SavedConfigs.OfType<DomainConfiguration>().ShouldHaveSingleItem();
        var implementation = gateway.SavedConfigs.OfType<TestRootConfiguration>().ShouldHaveSingleItem();
        implementation.TestRootDomainId.ShouldBe(domainRow.Id);
        implementation.Id.ShouldBe(domainRow.Id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public async Task DomainSaveRemovesTheDomainRowItMintedWhenTheImplementationIsRefused()
    {
        var gateway = new RecordingGateway { RefuseImplementation = true };
        var domain = new TestRootDomainProvider(GatewayProviderFor(gateway));
        domain.Register("Default", ImplementationProvider(gateway));

        var result = await domain.Save(
            new TestRootConfiguration(), "TestRoot", "Default", "Root", TestContext.Current.CancellationToken);

        // No transaction spans the two writes; a domain row left behind names an implementation that
        // does not exist.
        result.IsSuccess.ShouldBeFalse();
        var domainRow = gateway.SavedConfigs.OfType<DomainConfiguration>().ShouldHaveSingleItem();
        gateway.Deleted.ShouldContain(domainRow.Id);
    }

    private static TestRootImplementationProvider ImplementationProvider(IConfigurationGateway gateway)
        => new(GatewayProviderFor(gateway));

    // ========================================================================
    // Test infrastructure: a 3-level configuration hierarchy
    // ========================================================================

    /// <summary>The implementation record, owning a child collection.</summary>
    [GenerateMapper]
    public sealed class TestRootConfiguration : ITestRootImplementationConfiguration
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

        public string Domain { get; set; } = string.Empty;

        public string Implementation { get; set; } = "Default";

        /// <summary>The domain row this implementation belongs to (TestRootDomainProvider's table + "Id").</summary>
        public Guid TestRootDomainId { get; set; }

        public IList<TestOpConfiguration> Operations { get; set; } = [];
    }

    /// <summary>Operation holding its own child collection (mappings).</summary>
    [GenerateMapper]
    public sealed class TestOpConfiguration : IGenericConfiguration
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

        /// <summary>FK to the record that owns it, set by the cascade (Strip(TestRootConfiguration)+"Id").</summary>
        public Guid TestRootId { get; set; }

        public IList<TestMapConfiguration> Mappings { get; set; } = [];
    }

    /// <summary>Leaf field mapping.</summary>
    [GenerateMapper]
    public sealed class TestMapConfiguration : IGenericConfiguration
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

        /// <summary>FK to the parent operation, set by the cascade (Strip(TestOpConfiguration)+"Id").</summary>
        public Guid TestOpId { get; set; }
    }

    /// <summary>The provider under test: an implementation provider owns the cascade.</summary>
    public sealed class TestRootImplementationProvider
        : ImplementationProviderBase<TestRootConfiguration, ITestRootImplementationConfiguration>
    {
        public TestRootImplementationProvider(IConfigurationGatewayProvider gatewayProvider)
            : base(
                NullLogger<ImplementationProviderBase<TestRootConfiguration, ITestRootImplementationConfiguration>>.Instance,
                gatewayProvider,
                "PlatformConfiguration",
                "pipe",
                "TestRoot")
        {
        }
    }

    /// <summary>The domain provider: it owns the registry and dispatches by the row's implementation.</summary>
    public sealed class TestRootDomainProvider
        : DomainConfigurationProviderBase<ITestRootImplementationConfiguration>
    {
        public TestRootDomainProvider(IConfigurationGatewayProvider gatewayProvider)
            : base(
                NullLogger<DomainConfigurationProviderBase<ITestRootImplementationConfiguration>>.Instance,
                gatewayProvider,
                "PlatformConfiguration",
                "pipe",
                "TestRootDomain")
        {
        }
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
    /// Gateway test double that records every configuration record saved through it, so the test can
    /// assert which levels of the hierarchy were persisted.
    /// </summary>
    private sealed class RecordingGateway : IConfigurationGateway
    {
        /// <summary>The connection this fake stands in for.</summary>
        public string ConnectionName => "PlatformConfiguration";

        /// <summary>Targets this fake was asked to invalidate, in call order.</summary>
        public List<DataStoreTarget> Invalidated { get; } = [];

        public void InvalidateCachedResults(DataStoreTarget target) => Invalidated.Add(target);

        public List<object> SavedConfigs { get; } = [];

        /// <summary>Refuse the implementation row's insert, as the database does when it cannot resolve the domain RowId.</summary>
        public bool RefuseImplementation { get; init; }

        /// <summary>Ids named by delete commands, in call order.</summary>
        public List<Guid> Deleted { get; } = [];

        public IReadOnlyList<IDataStore> DataStores { get; } = [];

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, CancellationToken cancellationToken = default)
            => Execute<T>(command, default(DataStoreTarget)!, cancellationToken);

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken cancellationToken = default)
            => Execute<T>(command, target, cancellationToken);

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        {
            // A read asks for a sequence — the record is new, so nothing comes back.
            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return Task.FromResult(GenericResult<T>.Success((T)(object)Array.CreateInstance(typeof(T).GetGenericArguments()[0], 0)));

            if (command is ConfigurationDeleteCommand delete)
            {
                Deleted.Add(delete.Data);
                return Task.FromResult(GenericResult<T>.Success(default!));
            }

            if (RefuseImplementation && command is IConfigurationSaveCommand { InputData: TestRootConfiguration })
                return Task.FromResult(GenericResult<T>.Failure(new GenericMessage("Cannot insert the value NULL into column 'TestRootDomainRowId'")));

            // A save command exposes the saved POCO via IConfigurationSaveCommand.InputData.
            if (command is IConfigurationSaveCommand save && save.InputData is not null)
                SavedConfigs.Add(save.InputData);

            return Task.FromResult(GenericResult<T>.Success(default!));
        }

        public Task<IGenericResult> Execute(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        {
            // The cascade routes child saves through the non-generic Execute — record them too.
            if (command is IConfigurationSaveCommand save && save.InputData is not null)
                SavedConfigs.Add(save.InputData);
            return Task.FromResult<IGenericResult>(GenericResult.Success());
        }

        public Task<IGenericResult<IEnumerable<object>>> Execute(IDataCommand command, DataStoreTarget target, Type rowType, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<IEnumerable<object>>.Success(Array.Empty<object>()));

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataSetTarget target, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<T>.Failure(new GenericMessage("DataSet routing not supported in RecordingGateway test double")));

        public Task<IGenericResult<Fdw.Data.RowSources.Abstractions.IRecordSource<Fdw.Data.RowSources.Abstractions.DataRecord>>> OpenRecordSource(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => throw new System.NotImplementedException();

        public Task<IGenericResult<IDataGatewayTransaction>> BeginTransaction(string connectionName, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<IDataGatewayTransaction>.Failure(new GenericMessage("Transactions not supported in test double")));

        // The fixture addresses every call; a gateway's generic command surface carries no address,
        // so nothing here routes one and reaching these would be the test lying about the seam.
        string IPlatformService.Id => "RecordingGateway";

        string IPlatformService.ServiceType => "DataGateway";

        bool IPlatformService.IsAvailable => true;

        Task<IGenericResult<T>> IGenericService.Execute<T>(IGenericCommand command, CancellationToken cancellationToken)
            => throw new NotSupportedException("This fixture routes by target, never by a bare command.");

        Task<IGenericResult> IGenericService.Execute(IGenericCommand command, CancellationToken cancellationToken)
            => throw new NotSupportedException("This fixture routes by target, never by a bare command.");
    }

    /// <summary>
    /// Gateway test double that answers the domain read with one supplied row and every other read
    /// with nothing, so the missing-implementation path is exercised without a schema tree.
    /// </summary>
    private sealed class HeaderReturningGateway : IConfigurationGateway
    {
        /// <summary>The connection this fake stands in for.</summary>
        public string ConnectionName => "PlatformConfiguration";

        /// <summary>Targets this fake was asked to invalidate, in call order.</summary>
        public List<DataStoreTarget> Invalidated { get; } = [];

        public void InvalidateCachedResults(DataStoreTarget target) => Invalidated.Add(target);

        private readonly DomainConfiguration _row;

        public HeaderReturningGateway(DomainConfiguration row) => _row = row;

        public IReadOnlyList<IDataStore> DataStores { get; } = [];

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, CancellationToken cancellationToken = default)
            => Execute<T>(command, default(DataStoreTarget)!, cancellationToken);

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken cancellationToken = default)
            => Execute<T>(command, target, cancellationToken);

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        {
            if (typeof(T) == typeof(IEnumerable<DomainConfiguration>))
                return Task.FromResult(GenericResult<T>.Success((T)(object)new List<DomainConfiguration> { _row }));

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

        public Task<IGenericResult<Fdw.Data.RowSources.Abstractions.IRecordSource<Fdw.Data.RowSources.Abstractions.DataRecord>>> OpenRecordSource(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => throw new System.NotImplementedException();

        public Task<IGenericResult<IDataGatewayTransaction>> BeginTransaction(string connectionName, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<IDataGatewayTransaction>.Failure(new GenericMessage("Transactions not supported in test double")));

        // The fixture addresses every call; a gateway's generic command surface carries no address,
        // so nothing here routes one and reaching these would be the test lying about the seam.
        string IPlatformService.Id => "HeaderReturningGateway";

        string IPlatformService.ServiceType => "DataGateway";

        bool IPlatformService.IsAvailable => true;

        Task<IGenericResult<T>> IGenericService.Execute<T>(IGenericCommand command, CancellationToken cancellationToken)
            => throw new NotSupportedException("This fixture routes by target, never by a bare command.");

        Task<IGenericResult> IGenericService.Execute(IGenericCommand command, CancellationToken cancellationToken)
            => throw new NotSupportedException("This fixture routes by target, never by a bare command.");
    }

    private static AnyConnectionGateways GatewayProviderFor(IConfigurationGateway gateway)
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
