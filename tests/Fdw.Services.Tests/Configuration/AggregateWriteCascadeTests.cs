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
using Moq;
using Shouldly;
using Xunit;

using TestRootConfiguration = Fdw.Services.Tests.Configuration.RecursiveCascadeSaveTests.TestRootConfiguration;
using TestOpConfiguration = Fdw.Services.Tests.Configuration.RecursiveCascadeSaveTests.TestOpConfiguration;
using TestMapConfiguration = Fdw.Services.Tests.Configuration.RecursiveCascadeSaveTests.TestMapConfiguration;
using TestRootImplementationProvider = Fdw.Services.Tests.Configuration.RecursiveCascadeSaveTests.TestRootImplementationProvider;
using TestRootDomainProvider = Fdw.Services.Tests.Configuration.RecursiveCascadeSaveTests.TestRootDomainProvider;

using Fdw.Abstractions;
namespace Fdw.Services.Tests.Configuration;

/// <summary>
/// Pins the WRITE path of an implementation aggregate: the cascade runs on EVERY save (not just the
/// first), every row write is the same version-on-write <c>ConfigurationSaveCommand</c> shape, a
/// record with no children still writes, a domain save fails loud and writes nothing when nothing is
/// registered for the implementation it names, and delete retires the whole aggregate in REVERSE
/// order — deepest child first, the row it hangs from last.
/// </summary>
/// <remarks>
/// Why a separate file from RecursiveCascadeSaveTests: that file pins the read-side compose and the
/// happy-path save shape. This one pins the write-path MECHANISM — repeat-save, the single write
/// shape, the registration gate, and the delete cascade — each a distinct regression.
/// </remarks>
[Collection(nameof(ServicesTestCollection))]
public sealed class AggregateWriteCascadeTests
{
    private static TestRootImplementationProvider MakeProvider(RecordingGateway gateway)
        => new(GatewayProviderFor(gateway));

    // ========================================================================
    // 1. Cascade runs on a repeat save (the update case)
    // ========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public async Task SaveCascadesChildrenOnRepeatSaveNotJustFirst()
    {
        var mapping = new TestMapConfiguration { Id = Guid.NewGuid(), Name = "Map" };
        var operation = new TestOpConfiguration { Id = Guid.NewGuid(), Name = "Op", Mappings = { mapping } };
        var root = new TestRootConfiguration { Id = Guid.NewGuid(), Name = "Root", Operations = { operation } };

        var gateway = new RecordingGateway();
        var provider = MakeProvider(gateway);

        var first = await provider.Save(root, TestContext.Current.CancellationToken);
        var second = await provider.Save(root, TestContext.Current.CancellationToken);

        first.IsSuccess.ShouldBeTrue();
        second.IsSuccess.ShouldBeTrue();

        // Two full cascades means TWO rows per level — one per Save call — not one.
        gateway.SavedConfigs.OfType<TestRootConfiguration>().Count().ShouldBe(2);
        gateway.SavedConfigs.OfType<TestOpConfiguration>().Count().ShouldBe(2);
        gateway.SavedConfigs.OfType<TestMapConfiguration>().Count().ShouldBe(2);
    }

    // ========================================================================
    // 2. There is only one write shape
    // ========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public async Task SaveAlwaysUsesVersionOnWriteSaveCommandForTheRowNeverAPlainUpdate()
    {
        var root = new TestRootConfiguration { Id = Guid.NewGuid(), Name = "Root" };

        var gateway = new RecordingGateway();
        var provider = MakeProvider(gateway);

        await provider.Save(root, TestContext.Current.CancellationToken);
        await provider.Save(root, TestContext.Current.CancellationToken);

        var rowCommands = gateway.AllCommands
            .Where(c => c.Target.Container == "TestRoot")
            .Select(c => c.Command)
            .ToList();

        rowCommands.Count.ShouldBe(2);
        rowCommands.ShouldAllBe(c => c is ConfigurationSaveCommand<TestRootConfiguration>);
    }

    // ========================================================================
    // 3. A record with no children still writes
    // ========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public async Task SaveWritesTheRowWhenTheAggregateHasNoChildren()
    {
        var gateway = new RecordingGateway();
        var provider = MakeProvider(gateway);

        var result = await provider.Save(
            new TestRootConfiguration { Id = Guid.NewGuid(), Name = "Root" },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        gateway.SavedConfigs.OfType<TestRootConfiguration>().ShouldHaveSingleItem();
    }

    // ========================================================================
    // 4. Fail loud when nothing answers for the implementation named
    // ========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public async Task DomainSaveFailsLoudAndWritesNothingWhenNoProviderIsRegisteredForTheImplementation()
    {
        var gateway = new RecordingGateway();
        var domain = new TestRootDomainProvider(GatewayProviderFor(gateway));
        domain.Register("SomeOtherKind", MakeProvider(gateway));

        // The save names "Default" — registered above is "SomeOtherKind", so nothing answers for it.
        var result = await domain.Save(
            new TestRootConfiguration { Id = Guid.NewGuid() },
            "TestRoot",
            "Default",
            "Root",
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        gateway.AllCommands.ShouldBeEmpty();
        gateway.SavedConfigs.ShouldBeEmpty();
    }

    // ========================================================================
    // 5. Delete cascades in REVERSE order
    // ========================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public async Task DeleteRetiresDeepestChildFirstAndTheRowItHangsFromLast()
    {
        var mapping = new TestMapConfiguration { Id = Guid.NewGuid(), Name = "Map" };
        var operation = new TestOpConfiguration { Id = Guid.NewGuid(), Name = "Op", Mappings = { mapping } };
        var root = new TestRootConfiguration { Id = Guid.NewGuid(), Name = "Root" };

        // The read that Delete performs answers with the row and, for the child join, its operations —
        // the operation carries its own mapping, so the whole aggregate is in hand when retiring starts.
        var gateway = new RecordingGateway
        {
            DataStores = SchemaTree(),
            RootRow = root,
            ChildRows = { [typeof(TestOpConfiguration)] = [operation] },
        };

        var result = await MakeProvider(gateway).Delete(root.Id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();

        var deletes = gateway.AllCommands
            .Where(c => c.Command is ConfigurationDeleteCommand)
            .Select(c => (c.Target.Container, Id: ((ConfigurationDeleteCommand)c.Command).Data))
            .ToList();

        deletes.Count.ShouldBe(3);
        deletes[0].Container.ShouldBe("TestMap");
        deletes[0].Id.ShouldBe(mapping.Id);
        deletes[1].Container.ShouldBe("TestOp");
        deletes[1].Id.ShouldBe(operation.Id);
        deletes[2].Container.ShouldBe("TestRoot");
        deletes[2].Id.ShouldBe(root.Id);
    }

    // ========================================================================
    // Test infrastructure
    // ========================================================================

    /// <summary>
    /// The schema an implementation read needs: the implementation container, its foreign key to the
    /// domain container, and the domain's own physical (RowId) and logical (Id) keys — the join that
    /// resolves a domain id to this implementation's row.
    /// </summary>
    private static List<IDataStore> SchemaTree()
    {
        var domain = Container("TestRootDomain", KeyOn(KeyTypes.Physical, "RowId"), KeyOn(KeyTypes.Logical, "Id"));

        var foreignKey = new Mock<IContainerKey>();
        foreignKey.Setup(k => k.KeyType).Returns(KeyTypes.Foreign);
        foreignKey.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { KeyFieldOn("TestRootDomainRowId") });
        foreignKey.Setup(k => k.ReferencedContainer).Returns(domain);

        var root = Container("TestRoot", KeyOn(KeyTypes.Physical, "RowId"), KeyOn(KeyTypes.Logical, "Id"), foreignKey.Object);

        var path = new Mock<IDataNodePath>();
        path.Setup(p => p.Name).Returns("pipe");
        path.Setup(p => p.Containers).Returns(new List<IDataContainer> { root, domain });
        path.Setup(p => p.Container(It.Is<string>(n => string.Equals(n, "TestRoot", StringComparison.Ordinal))))
            .Returns(GenericResult<IDataContainer>.Success(root));
        path.Setup(p => p.Container(It.Is<string>(n => string.Equals(n, "TestRootDomain", StringComparison.Ordinal))))
            .Returns(GenericResult<IDataContainer>.Success(domain));
        path.Setup(p => p.Container(It.Is<string>(n =>
                !string.Equals(n, "TestRoot", StringComparison.Ordinal) &&
                !string.Equals(n, "TestRootDomain", StringComparison.Ordinal))))
            .Returns(GenericResult<IDataContainer>.Failure(new GenericMessage("container not found")));

        var store = new Mock<IDataStore>();
        store.Setup(s => s.Name).Returns("PlatformConfiguration");
        store.Setup(s => s.Paths).Returns(new List<IDataNodePath> { path.Object });
        store.Setup(s => s.Path(It.Is<string>(n => string.Equals(n, "pipe", StringComparison.Ordinal))))
            .Returns(GenericResult<IDataNodePath>.Success(path.Object));
        store.Setup(s => s.Path(It.Is<string>(n => !string.Equals(n, "pipe", StringComparison.Ordinal))))
            .Returns(GenericResult<IDataNodePath>.Failure(new GenericMessage("path not found")));

        return new List<IDataStore> { store.Object };
    }

    private static IDataContainer Container(string name, params IContainerKey[] keys)
    {
        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns(name);
        container.Setup(c => c.Keys).Returns(keys.ToList());
        container.Setup(c => c.Nodes).Returns(new List<IDataNode>());
        return container.Object;
    }

    private static IContainerKey KeyOn(KeyTypeBase keyType, string columnName)
    {
        var key = new Mock<IContainerKey>();
        key.Setup(k => k.KeyType).Returns(keyType);
        key.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { KeyFieldOn(columnName) });
        key.Setup(k => k.ReferencedContainer).Returns((IDataContainer?)null);
        return key.Object;
    }

    private static IContainerKeyField KeyFieldOn(string columnName)
    {
        var field = new Mock<IDataField>();
        field.Setup(f => f.Name).Returns(columnName);
        var keyField = new Mock<IContainerKeyField>();
        keyField.Setup(k => k.LocalField).Returns(field.Object);
        keyField.Setup(k => k.Ordinal).Returns(0);
        return keyField.Object;
    }

    /// <summary>
    /// Gateway test double that records every write command (save or delete, generic or non-generic)
    /// the provider issues, IN CALL ORDER, and answers reads from settable fields. Reads never land in
    /// <see cref="AllCommands"/> — only writes count toward the cascade order/shape assertions.
    /// </summary>
    private sealed class RecordingGateway : IConfigurationGateway
    {
        /// <summary>The connection this fake stands in for.</summary>
        public string ConnectionName => "PlatformConfiguration";

        /// <summary>Targets this fake was asked to invalidate, in call order.</summary>
        public List<DataStoreTarget> Invalidated { get; } = [];

        public void InvalidateCachedResults(DataStoreTarget target) => Invalidated.Add(target);

        /// <summary>Every write command the gateway received, in call order.</summary>
        public List<(IDataCommand Command, DataStoreTarget Target)> AllCommands { get; } = [];

        /// <summary>Every configuration POCO saved through a ConfigurationSaveCommand, in call order.</summary>
        public List<object> SavedConfigs { get; } = [];

        /// <summary>The row returned by any implementation read. Null = "not found".</summary>
        public TestRootConfiguration? RootRow { get; init; }

        /// <summary>Child rows answered by row type when the compose issues its child join.</summary>
        public Dictionary<Type, object[]> ChildRows { get; } = [];

        /// <inheritdoc/>
        public IReadOnlyList<IDataStore> DataStores { get; init; } = [];

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, CancellationToken cancellationToken = default)
            => Execute<T>(command, default(DataStoreTarget)!, cancellationToken);

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken cancellationToken = default)
            => Execute<T>(command, target, cancellationToken);

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        {
            // A read asks for IEnumerable<TConfig> — answer from the configured row. This is a QUERY,
            // never recorded as a write.
            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var elementType = typeof(T).GetGenericArguments()[0];
                if (elementType == typeof(TestRootConfiguration))
                    return Task.FromResult(GenericResult<T>.Success((T)(object)(RootRow is null
                        ? new List<TestRootConfiguration>()
                        : new List<TestRootConfiguration> { RootRow })));
                return Task.FromResult(GenericResult<T>.Success((T)(object)Array.CreateInstance(elementType, 0)));
            }

            // Anything else is a row SAVE or a row DELETE — record it in call order.
            AllCommands.Add((command, target));
            if (command is IConfigurationSaveCommand save && save.InputData is not null)
                SavedConfigs.Add(save.InputData);
            return Task.FromResult(GenericResult<T>.Success(default!));
        }

        public Task<IGenericResult> Execute(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        {
            // Every CHILD save/delete routes through the non-generic Execute — record it in the SAME
            // list as the row's own writes so cross-level ordering is observable from one sequence.
            AllCommands.Add((command, target));
            if (command is IConfigurationSaveCommand save && save.InputData is not null)
                SavedConfigs.Add(save.InputData);
            return Task.FromResult<IGenericResult>(GenericResult.Success());
        }

        public Task<IGenericResult<IEnumerable<object>>> Execute(IDataCommand command, DataStoreTarget target, Type rowType, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<IEnumerable<object>>.Success(
                ChildRows.TryGetValue(rowType, out var rows) ? rows : Array.Empty<object>()));

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataSetTarget target, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<T>.Failure(new GenericMessage("DataSet routing not supported in RecordingGateway test double")));

        public Task<IGenericResult<Fdw.Data.RowSources.Abstractions.IRecordSource<Fdw.Data.RowSources.Abstractions.DataRecord>>> OpenRecordSource(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

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
