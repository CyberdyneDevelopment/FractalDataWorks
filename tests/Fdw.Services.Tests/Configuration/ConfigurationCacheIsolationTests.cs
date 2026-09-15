using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Commands;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;
using Fdw.Services.Data;

using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
namespace Fdw.Services.Tests.Configuration;

/// <summary>
/// Verifies repeated domain reads cannot replace the cached implementation identity used by child joins.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public sealed class ConfigurationCacheIsolationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Priority", "P0")]
    [Trait("Category", "Cascade")]
    public async Task RepeatedDomainReadsPreserveDatastorePaths(bool cacheRows)
    {
        // Arrange — a store with one path holding one container, linked by physical RowId FKs, exactly
        // as the seeded AuthDb → auth → RevokedAccessToken rows are in ConfigurationDb's data.* tables.
        // RowId is never set: it is DB-managed and absent from every config POCO; only the durable Id is.
        PocoMapperCollection.RegisterMember(new DataStoreImplementationConfigurationPocoMapper());
        PocoMapperCollection.RegisterMember(new DataPathConfigurationPocoMapper());
        PocoMapperCollection.RegisterMember(new DataContainerConfigurationPocoMapper());
        var store = new DataStoreImplementationConfiguration { Id = Guid.NewGuid(), Name = "AuthDb" };
        var path = new DataPathConfiguration { Id = Guid.NewGuid(), Name = "auth" };
        var container = new DataContainerConfiguration { Id = Guid.NewGuid(), Name = "RevokedAccessToken" };

        var gateway = new ComposingReadGateway(store, cacheRows);
        gateway.Seed(typeof(DataPathConfiguration), path);
        gateway.Seed(typeof(DataContainerConfiguration), container);

        // Why the named provider and not the base: the base is abstract now, and the cascade under test
        // lives in ImplementationProviderBase, which DataStoreImplementationConfigurationProvider closes.
        var provider = new DataStoreImplementationConfigurationProvider(
            NullLogger<DataStoreImplementationConfigurationProvider>.Instance,
            GatewayProviderFor(gateway),
            "ConfigurationDb");

        // Act -- an implementation provider reads by the owning domain row's durable id, not by name.
        var domain = new DataStoreConfigurationProvider(NullLogger<DataStoreConfigurationProvider>.Instance, GatewayProviderFor(gateway), "ConfigurationDb");
        domain.Register("MsSql", provider);
        var implementationId = store.Id;
        var first = await domain.Get("AuthDb", TestContext.Current.CancellationToken);
        first.IsSuccess.ShouldBeTrue(first.CurrentMessage?.ToString());
        first.Value!.Paths.Count.ShouldBe(1);
        store.Id.ShouldBe(implementationId);
        first.Value.ShouldNotBeSameAs(store);
        first.Value.LastDiscoveredAt.ShouldBeNull();
        var result = await domain.Get("AuthDb", TestContext.Current.CancellationToken);

        // Assert — the FULL aggregate composed: DataStore → Paths(1) → Containers(1).
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Paths.ShouldHaveSingleItem();
        result.Value.Paths[0].Name.ShouldBe("auth");
        result.Value.Paths[0].Containers.ShouldHaveSingleItem();
        result.Value.Paths[0].Containers[0].Name.ShouldBe("RevokedAccessToken");

        // Assert — the cascade keyed children via the metadata-driven JOIN to each owner (RowId is invisible;
        // the join filters by the owner's durable Id). At least one typed child read ran per owner level.
        gateway.ChildReads.ShouldNotBeEmpty();
        gateway.ChildReads.ShouldContain(typeof(DataPathConfiguration));
        gateway.ChildReads.ShouldContain(typeof(DataContainerConfiguration));
    }

    /// <summary>
    /// Gateway double: returns the store header on the by-name read and seeded child rows on the by-type
    /// child read, recording the child <see cref="Type"/> each child query requested so the test can assert
    /// the cascade ran a JOIN-based read per owner level. <see cref="DataStores"/> carries the schema tree
    /// with each owner container's Physical (RowId) + Logical (Id) keys — the column names the cascade
    /// resolves to build the JOIN. DataPath rows are returned only when the generated JOIN filters by the implementation Id.
    /// </summary>
    private sealed class ComposingReadGateway : IConfigurationGateway
    {
        /// <summary>The connection this fake stands in for.</summary>
        public string ConnectionName => "PlatformConfiguration";

        /// <summary>Targets this fake was asked to invalidate, in call order.</summary>
        public List<DataStoreTarget> Invalidated { get; } = [];

        public void InvalidateCachedResults(DataStoreTarget target) => Invalidated.Add(target);

        private readonly bool _cacheRows;
        private readonly Guid _originalId;
        private readonly DataStoreImplementationConfiguration _header;
        private readonly Dictionary<Type, List<object>> _childrenByType = new();
        private readonly IReadOnlyList<IDataStore> _stores;

        public ComposingReadGateway(DataStoreImplementationConfiguration header, bool cacheRows)
        {
            _header = header; _originalId = header.Id; _cacheRows = cacheRows;
            _stores = [BuildTree()];
        }

        public List<Type> ChildReads { get; } = [];

        public IReadOnlyList<IDataStore> DataStores => _stores;

        public void Seed(Type rowType, params object[] rows) => _childrenByType[rowType] = rows.ToList();

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, CancellationToken cancellationToken = default)
            => Execute<T>(command, default(DataStoreTarget)!, cancellationToken);

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken cancellationToken = default)
            // Why: test double — useCache not exercised in compose-read tests; delegates to existing implementation.
            => Execute<T>(command, target, cancellationToken);

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        {
            if (typeof(T) == typeof(IEnumerable<DomainConfiguration>)) return Task.FromResult(GenericResult<T>.Success((T)(object)new[] { new DomainConfiguration { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "AuthDb", Implementation = "MsSql" } }));
            if (typeof(T) == typeof(IEnumerable<DataStoreImplementationConfiguration>))
                return Task.FromResult(GenericResult<T>.Success((T)(object)new List<DataStoreImplementationConfiguration> { _cacheRows ? _header : new DataStoreImplementationConfiguration { Id = _originalId, Name = _header.Name } }));

            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return Task.FromResult(GenericResult<T>.Success((T)(object)Array.CreateInstance(typeof(T).GetGenericArguments()[0], 0)));

            return Task.FromResult(GenericResult<T>.Success(default!));
        }

        public Task<IGenericResult> Execute(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => Task.FromResult<IGenericResult>(GenericResult.Success());

        // The typed-list child read — record the requested row-type and return the seeded rows, regardless of
        // the JOIN filter (the new cascade joins on owner.Id rather than a single materialized RowId value).
        public Task<IGenericResult<IEnumerable<object>>> Execute(IDataCommand command, DataStoreTarget target, Type rowType, CancellationToken cancellationToken = default)
        {
            ChildReads.Add(rowType);
            if (rowType == typeof(DataPathConfiguration) && !HasOwnerId(((QueryCommand<object>)command).Filter?.Root, _originalId)) return Task.FromResult(GenericResult<IEnumerable<object>>.Success(Array.Empty<object>()));
            var rows = _childrenByType.TryGetValue(rowType, out var list) ? list : [];
            return Task.FromResult(GenericResult<IEnumerable<object>>.Success(rows));
        }

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataSetTarget target, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("This fixture only routes container commands.");

        // Why: streaming record-source cursor is not exercised by this test double.
        public Task<IGenericResult<Fdw.Data.RowSources.Abstractions.IRecordSource<Fdw.Data.RowSources.Abstractions.DataRecord>>> OpenRecordSource(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IGenericResult<IDataGatewayTransaction>> BeginTransaction(string connectionName, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Transactions are not used by configuration reads.");

        // Why: the schema tree mirrors ConfigurationDb's data.* schema. Each OWNER container (DataStore,
        // DataPath) carries the Physical (RowId) + Logical (Id) keys the cascade resolves to build the
        // child JOIN; the leaf DataContainer is given the same keys so a nested recurse into it never
        // mis-resolves. Modeled on the calc-domain provider test's BuildTree.
        private static bool HasOwnerId(object? node, Guid id)
        {
            if (node is FilterCondition condition)
                return condition.PropertyName == "DataStoreImplementation.Id" && Equals(condition.Value, id);
            if (node is FilterGroup group)
                return group.Nodes.Any(child => HasOwnerId(child, id));
            return false;
        }
        private static IDataStore BuildTree()
        {
            var containerContainer = Container("DataContainer", [],
                [Key("Physical", "PK_DataContainer", "RowId", null), Key("Logical", "AK_DataContainer", "Id", null)]);

            // DataPath owns Containers (joined on DataPathRowId) — it must carry Physical + Logical keys.
            var pathContainer = Container("DataPath",
                [Binding("DataPathRowId", containerContainer)],
                [Key("Physical", "PK_DataPath", "RowId", null), Key("Logical", "AK_DataPath", "Id", null)]);

            // DataStore owns Paths (joined on DataStoreRowId) — the top-level owner; needs both keys too.
            var storeContainer = Container("DataStore",
                [Binding("DataStoreRowId", pathContainer)],
                [Key("Physical", "PK_DataStore", "RowId", null), Key("Logical", "AK_DataStore", "Id", null)]);

            var implementationContainer = Container("DataStoreImplementation", [], [Key("Physical", "PK_Impl", "RowId", null), Key("Logical", "AK_Impl", "Id", null), Key("Foreign", "FK_Impl", "DataStoreRowId", storeContainer)]);
            var containers = new List<IDataContainer> { storeContainer, implementationContainer, pathContainer, containerContainer };

            var path = new Mock<IDataNodePath>();
            path.Setup(p => p.Name).Returns("data");
            path.Setup(p => p.Containers).Returns(containers);
            path.Setup(p => p.Container(It.IsAny<string>())).Returns((string n) =>
            {
                var c = containers.FirstOrDefault(x => string.Equals(x.Name, n, StringComparison.Ordinal));
                return c is null ? GenericResult<IDataContainer>.Failure(Fdw.Services.Configuration.Logging.DefaultConfigurationProviderLog.ContainerNotFoundInStore(NullLogger.Instance, "Test", n, "ConfigurationDb")) : GenericResult<IDataContainer>.Success(c);
            });
            foreach (var c in containers)
                Mock.Get(c).Setup(x => x.Parent).Returns(path.Object);

            var store = new Mock<IDataStore>();
            store.Setup(s => s.Name).Returns("ConfigurationDb");
            store.Setup(s => s.Paths).Returns(new List<IDataNodePath> { path.Object });
            store.Setup(s => s.Path(It.IsAny<string>())).Returns((string n) =>
                string.Equals(n, "data", StringComparison.Ordinal)
                    ? GenericResult<IDataNodePath>.Success(path.Object)
                    : GenericResult<IDataNodePath>.Failure(Fdw.Services.Configuration.Logging.DefaultConfigurationProviderLog.ContainerNotFoundInStore(NullLogger.Instance, "Test", n, "ConfigurationDb")));
            return store.Object;
        }

        private static IDataContainer Container(
            string name, IReadOnlyList<ReferencingKeyBinding> referencing, IReadOnlyList<IContainerKey>? keys)
        {
            var c = new Mock<IDataContainer>();
            c.Setup(x => x.Name).Returns(name);
            c.Setup(x => x.Keys).Returns(keys ?? new List<IContainerKey>());
            c.Setup(x => x.Nodes).Returns(new List<IDataNode>());
            c.Setup(x => x.ReferencingKeys).Returns(
                GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success(referencing));
            return c.Object;
        }

        private static ReferencingKeyBinding Binding(string fkColumn, IDataContainer owner)
        {
            var field = new Mock<IDataField>();
            field.Setup(f => f.Name).Returns(fkColumn);
            var keyField = new Mock<IContainerKeyField>();
            keyField.Setup(k => k.LocalField).Returns(field.Object);
            var key = new Mock<IContainerKey>();
            key.Setup(k => k.KeyName).Returns($"FK_{fkColumn}_{owner.Name}");
            key.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { keyField.Object });
            return new ReferencingKeyBinding(key.Object, owner);
        }

        private static IContainerKey Key(string keyType, string keyName, string localField, IDataContainer? referenced)
        {
            var field = new Mock<IDataField>();
            field.Setup(f => f.Name).Returns(localField);
            var keyField = new Mock<IContainerKeyField>();
            keyField.Setup(k => k.LocalField).Returns(field.Object);
            // Why: KeyType is the abstract KeyTypeBase TypeOption — use the real concrete instances so
            // FindKeyFieldName reads the genuine Name ("Physical"/"Logical").
            KeyTypeBase kt = keyType switch
            {
                "Physical" => new PhysicalKeyType(),
                "Logical" => new LogicalKeyType(),
                "Foreign" => new ForeignKeyType(),
                _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, "unsupported key type in test")
            };
            var key = new Mock<IContainerKey>();
            key.Setup(k => k.KeyType).Returns(kt);
            key.Setup(k => k.KeyName).Returns(keyName);
            key.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { keyField.Object });
            key.Setup(k => k.ReferencedContainer).Returns(referenced);
            return key.Object;
        }

        // The fixture addresses every call; a gateway's generic command surface carries no address,
        // so nothing here routes one and reaching these would be the test lying about the seam.
        string IPlatformService.Id => "ComposingReadGateway";

        string IPlatformService.ServiceType => "DataGateway";

        bool IPlatformService.IsAvailable => true;

        Task<IGenericResult<T>> IGenericService.Execute<T>(IGenericCommand command, CancellationToken cancellationToken)
            => throw new NotSupportedException("This fixture routes by target, never by a bare command.");

        Task<IGenericResult> IGenericService.Execute(IGenericCommand command, CancellationToken cancellationToken)
            => throw new NotSupportedException("This fixture routes by target, never by a bare command.");
    }

    // Why the gateway answers for any connection: these tests exercise what a provider does with
    // its gateway, not which one it selects.
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











