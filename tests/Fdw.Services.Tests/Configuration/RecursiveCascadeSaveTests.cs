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
using Moq;
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
/// conn.MsSqlConnectionLimit.MsSqlConnectionConfigurationId).
/// </remarks>
[Collection(nameof(ServicesTestCollection))]
public sealed class RecursiveCascadeSaveTests
{

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
    public async Task GetByNameRefusesANameTwoVisibleRowsCarry()
    {
        var first = new DomainConfiguration { Id = Guid.NewGuid(), Name = "Root", Domain = "TestRoot", Implementation = "Default" };
        var second = new DomainConfiguration { Id = Guid.NewGuid(), Name = "Root", Domain = "TestRoot", Implementation = "Default" };
        var gateway = new HeaderReturningGateway(first, second);
        var domain = new TestRootDomainProvider(GatewayProviderFor(gateway));
        domain.Register("Default", ImplementationProvider(gateway));

        var result = await domain.Get("Root", TestContext.Current.CancellationToken);

        // Two tenants' rows can carry one name; answering with the first back would act on one the caller did not
        // choose (FDW-1101). The refusal names both ids so the caller can address one.
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain(first.Id.ToString("D"));
        result.CurrentMessage.ShouldContain(second.Id.ToString("D"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public async Task DeleteByNameRefusesANameTwoVisibleRowsCarry()
    {
        var gateway = new HeaderReturningGateway(
            new DomainConfiguration { Id = Guid.NewGuid(), Name = "Root", Domain = "TestRoot", Implementation = "Default" },
            new DomainConfiguration { Id = Guid.NewGuid(), Name = "Root", Domain = "TestRoot", Implementation = "Default" });
        var domain = new TestRootDomainProvider(GatewayProviderFor(gateway));
        domain.Register("Default", ImplementationProvider(gateway));

        var result = await domain.Delete("Root", TestContext.Current.CancellationToken);

        // Deleting whichever row came back first would retire another tenant's configuration.
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

        // The implementation row hangs from the domain row by the Foreign key its container declares,
        // filled from the key values the domain row's write returned. Left empty, the database refused
        // the implementation row with a NULL TestRootDomainRowId.
        result.IsSuccess.ShouldBeTrue();
        var domainRow = gateway.SavedConfigs.OfType<DomainConfiguration>().ShouldHaveSingleItem();
        var implementation = gateway.SavedConfigs.OfType<TestRootConfiguration>().ShouldHaveSingleItem();
        implementation.Id.ShouldBe(domainRow.Id);
        gateway.SavedFields[implementation]["TestRootDomainRowId"].ShouldBe(gateway.RowIdOf(domainRow));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public async Task DomainSaveAbandonsTheDomainRowItMintedWhenTheImplementationIsRefused()
    {
        var gateway = new RecordingGateway { RefuseImplementation = true };
        var domain = new TestRootDomainProvider(GatewayProviderFor(gateway));
        domain.Register("Default", ImplementationProvider(gateway));

        var result = await domain.Save(
            new TestRootConfiguration(), "TestRoot", "Default", "Root", TestContext.Current.CancellationToken);

        // Both rows are written in one unit, so the refused implementation takes the domain row with
        // it. The row is abandoned rather than deleted afterwards: the removal this replaces was
        // itself a write that could fail, and when it did the row stayed, naming an implementation
        // that does not exist.
        result.IsSuccess.ShouldBeFalse();
        gateway.Transaction.RolledBack.ShouldBeTrue();
        gateway.Transaction.Committed.ShouldBeFalse();
        gateway.Deleted.ShouldBeEmpty();
    }

    private static IEnumerable<FilterCondition> Conditions(IFilterNode node)
    {
        if (node is FilterCondition condition)
        {
            yield return condition;
            yield break;
        }

        if (node is FilterGroup group)
        {
            foreach (var child in group.Nodes)
                foreach (var nested in Conditions(child))
                    yield return nested;
        }
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

        // The domain container owns the implementation container through the Foreign key the
        // implementation declares against the domain's Physical key, as the DDL would.
        public IReadOnlyList<IDataStore> DataStores { get; } = [TestRootStore()];

        /// <summary>The field values each save command carried, by the record it saved.</summary>
        public Dictionary<object, IReadOnlyDictionary<string, object?>> SavedFields { get; } = new(ReferenceEqualityComparer.Instance);

        private readonly Dictionary<object, int> _rowIds = new(ReferenceEqualityComparer.Instance);

        /// <summary>The RowId the write of <paramref name="record"/> answered with.</summary>
        public int RowIdOf(object record) => _rowIds[record];

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, CancellationToken cancellationToken = default)
            => Execute<T>(command, default(DataStoreTarget)!, cancellationToken);

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken cancellationToken = default)
            => Execute<T>(command, target, cancellationToken);

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        {
            // A read asks for a sequence — the record is new, so nothing comes back.
            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return Task.FromResult(GenericResult<T>.Success((T)(object)Array.CreateInstance(typeof(T).GetGenericArguments()[0], 0)));

            if (command is ConfigurationRetireCommand retire)
            {
                if (retire.KeyValues.Count > 0 && retire.KeyValues[0] is Guid id)
                    Deleted.Add(id);
                return Retired<T>();
            }

            if (RefuseImplementation && command is IConfigurationSaveCommand { InputData: TestRootConfiguration })
                return Task.FromResult(GenericResult<T>.Failure(new GenericMessage("Cannot insert the value NULL into column 'TestRootDomainRowId'")));

            // A save command exposes the saved POCO via IConfigurationSaveCommand.InputData.
            if (command is IConfigurationSaveCommand save && save.InputData is not null)
            {
                SavedConfigs.Add(save.InputData);
                SavedFields[save.InputData] = save.FieldValues;
            }

            return Wrote<T>((command as IConfigurationSaveCommand)?.InputData);
        }

        // A write answers with the key values of the row it wrote, as the database's OUTPUT does.
        private int _rowId;

        private Task<IGenericResult<T>> Wrote<T>(object? data)
        {
            if (typeof(T) != typeof(ConfigurationWriteResult))
                return Task.FromResult(GenericResult<T>.Success(default!));
            var written = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["RowId"] = ++_rowId,
                ["Id"] = (data as IGenericConfiguration)?.Id,
            };
            if (data is not null) _rowIds[data] = _rowId;
            return Task.FromResult(GenericResult<T>.Success((T)(object)new ConfigurationWriteResult([], written)));
        }

        private static IDataStore TestRootStore()
        {
            var implementation = Container("TestRoot", [],
                [Key("Physical", "RowId"), Key("Logical", "Id"), Key("Foreign", "TestRootDomainRowId")]);
            var domain = Container("TestRootDomain", [Owns(implementation, "TestRootDomainRowId")],
                [Key("Physical", "RowId"), Key("Logical", "Id")]);
            IReadOnlyList<IDataContainer> containers = [domain, implementation];

            var path = new Mock<IDataNodePath>();
            path.Setup(p => p.Name).Returns("pipe");
            path.Setup(p => p.Containers).Returns(containers);
            path.Setup(p => p.Container(It.IsAny<string>())).Returns((string name) =>
                containers.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.Ordinal)) is { } found
                    ? GenericResult<IDataContainer>.Success(found)
                    : GenericResult<IDataContainer>.Failure(new GenericMessage($"No container '{name}'")));
            var store = new Mock<IDataStore>();
            store.Setup(s => s.Name).Returns("PlatformConfiguration");
            store.Setup(s => s.Path(It.IsAny<string>())).Returns(GenericResult<IDataNodePath>.Success(path.Object));
            return store.Object;
        }

        private static IDataContainer Container(string name, IReadOnlyList<ReferencingKeyBinding> referencing, IReadOnlyList<IContainerKey> keys)
        {
            var container = new Mock<IDataContainer>();
            container.Setup(c => c.Name).Returns(name);
            container.Setup(c => c.Keys).Returns(keys);
            container.Setup(c => c.ReferencingKeys).Returns(GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success(referencing));
            return container.Object;
        }

        // The key the owned container declares, naming the owner's Physical key (RowId) it references.
        private static ReferencingKeyBinding Owns(IDataContainer owned, string foreignKeyColumn)
        {
            var referenced = new Mock<IDataField>();
            referenced.Setup(f => f.Name).Returns("RowId");
            return new ReferencingKeyBinding(Key("Foreign", foreignKeyColumn, referenced.Object), owned);
        }

        private static IContainerKey Key(string keyType, string column, IDataField? referenced = null)
        {
            var local = new Mock<IDataField>();
            local.Setup(f => f.Name).Returns(column);
            var keyField = new Mock<IContainerKeyField>();
            keyField.Setup(k => k.LocalField).Returns(local.Object);
            keyField.Setup(k => k.ReferencedField).Returns(referenced);
            KeyTypeBase type = keyType switch
            {
                "Physical" => new PhysicalKeyType(),
                "Logical" => new LogicalKeyType(),
                "Foreign" => new ForeignKeyType(),
                _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, "unsupported key type in test"),
            };
            var key = new Mock<IContainerKey>();
            key.Setup(k => k.KeyType).Returns(type);
            key.Setup(k => k.KeyName).Returns($"{keyType}_{column}");
            key.Setup(k => k.KeyFields).Returns([keyField.Object]);
            return key.Object;
        }

        private static Task<IGenericResult<T>> Retired<T>()
            => Task.FromResult(GenericResult<T>.Success(typeof(T) == typeof(ConfigurationWriteResult)
                ? (T)(object)new ConfigurationWriteResult([], null)
                : default!));

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
            => Task.FromResult(GenericResult<IDataGatewayTransaction>.Success(Transaction));

        // Held so a test can assert whether the unit was committed or abandoned.
        public RecordingTransaction Transaction => _transaction ??= new RecordingTransaction(this);

        private RecordingTransaction? _transaction;

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

        private readonly IReadOnlyList<DomainConfiguration> _rows;

        public HeaderReturningGateway(params DomainConfiguration[] rows) => _rows = rows;

        public IReadOnlyList<IDataStore> DataStores { get; } = [];

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, CancellationToken cancellationToken = default)
            => Execute<T>(command, default(DataStoreTarget)!, cancellationToken);

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken cancellationToken = default)
            => Execute<T>(command, target, cancellationToken);

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        {
            if (typeof(T) == typeof(IEnumerable<DomainConfiguration>))
                return Task.FromResult(GenericResult<T>.Success((T)(object)new List<DomainConfiguration>(_rows)));

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
            => Task.FromResult(GenericResult<IDataGatewayTransaction>.Success(Transaction));

        // Held so a test can assert whether the unit was committed or abandoned.
        public RecordingTransaction Transaction => _transaction ??= new RecordingTransaction(this);

        private RecordingTransaction? _transaction;

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
