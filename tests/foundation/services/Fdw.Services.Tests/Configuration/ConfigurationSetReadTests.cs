using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Moq;
using Shouldly;
using Xunit;
using static Fdw.Services.Tests.Configuration.RecursiveCascadeSaveTests;

namespace Fdw.Services.Tests.Configuration;

/// <summary>
/// A list of configuration rows is read as a set: the implementation rows and each child table once per level, whatever the number of rows (FDW-3906).
/// </summary>
/// <remarks>
/// Why a counting store: the defect was the number of statements, not the answer. A list of three thousand data sets ran a statement for each row's
/// implementation and one for each child collection of each row, which took minutes. These tests hold the number of statements to the number of
/// tables, and the answer to what a read of each row alone gave.
/// </remarks>
[Collection(nameof(ServicesTestCollection))]
public sealed class ConfigurationSetReadTests
{
    private const int Levels = ConfigurationLevels.WholeAggregate;

    // The most keys one statement carries in the test that reads past it; the gateway provider states it, so a set one larger is read in two.
    private const int SmallKeyChunkSize = 4;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Performance")]
    public async Task AListReadsEachTableOnceWhateverTheNumberOfRows()
    {
        // Operations hang from two of the roots, so the mappings level has rows to read under; a level whose parents have none is not read.
        var store = new SetStore().WithRoots(3);
        store.Gateway.Add("TestOp", Row(21, "TestRootRowId", 11, "op1a"), Row(22, "TestRootRowId", 11, "op1b"), Row(23, "TestRootRowId", 12, "op2"));

        var result = await store.Domain.Get(Levels, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Count.ShouldBe(3);
        store.Gateway.Reads("TestRoot").ShouldBe(1);
        store.Gateway.Reads("TestOp").ShouldBe(1);
        store.Gateway.Reads("TestMap").ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Performance")]
    public async Task ASingleReadByNameReadsEachTableOnceWhateverTheNumberOfChildren()
    {
        // FDW-4254: one row with many descendants. Read row by row, this made a statement for each operation's mappings (a datastore of 500
        // containers made ~2470 statements, FDW-4252); the cascade of one row is the set read of a set of one, so it is a statement per table.
        const int operations = 25;
        var store = new SetStore().WithRoots(1);
        for (var i = 0; i < operations; i++)
        {
            store.Gateway.Add("TestOp", Row(100 + i, "TestRootRowId", 11, $"op{i}"));
            store.Gateway.Add("TestMap", Row(200 + 2 * i, "TestOpRowId", 100 + i, $"m{i}a"), Row(201 + 2 * i, "TestOpRowId", 100 + i, $"m{i}b"));
        }

        var result = await store.Domain.Get("root1", Levels, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var root = result.Value.ShouldBeOfType<TestRootConfiguration>();
        root.Operations.Count.ShouldBe(operations);
        root.Operations.ShouldAllBe(op => op.Mappings.Count == 2);
        store.Gateway.Reads("TestRoot").ShouldBe(1);
        store.Gateway.Reads("TestOp").ShouldBe(1);
        store.Gateway.Reads("TestMap").ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Performance")]
    public async Task ANarrowedReadLoadsOnlyTheNamedRowsAndTheChildrenOfThose()
    {
        // FDW-4328: a command that names two containers of a store of 500 reads those and what hangs from them, not the other 498.
        var store = new SetStore().WithRoots(1);
        store.Gateway.Add("TestOp", Row(21, "TestRootRowId", 11, "op1a"), Row(22, "TestRootRowId", 11, "op1b"), Row(23, "TestRootRowId", 11, "op1c"));
        store.Gateway.Add("TestMap", Row(31, "TestOpRowId", 21, "m1a"), Row(32, "TestOpRowId", 22, "m1b"), Row(33, "TestOpRowId", 23, "m1c"));
        var narrowing = new ChildNarrowing(typeof(TestOpConfiguration), "Name", ["op1a", "op1c"]);

        var result = await store.Domain.Get("root1", Levels, narrowing, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var root = result.Value.ShouldBeOfType<TestRootConfiguration>();
        root.Operations.Select(op => op.Name).OrderBy(name => name, StringComparer.Ordinal).ShouldBe(["op1a", "op1c"]);
        root.Operations.SelectMany(op => op.Mappings).Select(map => map.Name).OrderBy(name => name, StringComparer.Ordinal).ShouldBe(["m1a", "m1c"]);
        store.Gateway.Reads("TestRoot").ShouldBe(1);
        store.Gateway.Reads("TestOp").ShouldBe(1);
        store.Gateway.Reads("TestMap").ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ANarrowingOfAnotherKindOfRowNarrowsNothing()
    {
        var store = new SetStore().WithRoots(1);
        store.Gateway.Add("TestOp", Row(21, "TestRootRowId", 11, "op1a"), Row(22, "TestRootRowId", 11, "op1b"));
        var narrowing = new ChildNarrowing(typeof(TestMapConfiguration), "Name", ["nothing-by-this-name"]);

        var result = await store.Domain.Get("root1", Levels, narrowing, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldBeOfType<TestRootConfiguration>().Operations.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ASingleReadByIdGivesTheSameRowsAsTheListRead()
    {
        // One root: the fake domain read answers every domain row it holds whatever the id, so a second would make the single read ambiguous.
        var store = new SetStore().WithRoots(1);
        store.Gateway.Add("TestOp", Row(21, "TestRootRowId", 11, "op1a"), Row(22, "TestRootRowId", 11, "op1b"));
        store.Gateway.Add("TestMap", Row(31, "TestOpRowId", 21, "m1a"), Row(32, "TestOpRowId", 22, "m1b"));

        var listed = await store.Domain.Get(Levels, TestContext.Current.CancellationToken);
        var single = await store.Domain.Get(store.DomainIds[0], Levels, TestContext.Current.CancellationToken);

        listed.IsSuccess.ShouldBeTrue(listed.CurrentMessage);
        single.IsSuccess.ShouldBeTrue(single.CurrentMessage);
        var fromList = listed.Value!.Cast<TestRootConfiguration>().Single(root => root.Id == store.DomainIds[0]);
        var alone = single.Value.ShouldBeOfType<TestRootConfiguration>();
        alone.Name.ShouldBe(fromList.Name);
        alone.Operations.Select(op => op.Name).ShouldBe(fromList.Operations.Select(op => op.Name));
        alone.Operations.SelectMany(op => op.Mappings).Select(map => map.Name).ShouldBe(fromList.Operations.SelectMany(op => op.Mappings).Select(map => map.Name));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task EveryChildGoesToTheRowItHangsFrom()
    {
        var store = new SetStore().WithRoots(3);
        store.Gateway.Add("TestOp", Row(21, "TestRootRowId", 11, "op1a"), Row(22, "TestRootRowId", 11, "op1b"), Row(23, "TestRootRowId", 12, "op2"));
        store.Gateway.Add("TestMap", Row(31, "TestOpRowId", 21, "m1a"), Row(32, "TestOpRowId", 23, "m2"));

        var result = await store.Domain.Get(Levels, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var roots = result.Value!.Cast<TestRootConfiguration>().ToDictionary(root => root.Name);
        roots["root1"].Operations.Select(op => op.Name).ShouldBe(["op1a", "op1b"]);
        roots["root2"].Operations.Select(op => op.Name).ShouldBe(["op2"]);
        roots["root3"].Operations.ShouldBeEmpty();
        roots["root1"].Operations.Single(op => op.Name == "op1a").Mappings.Select(map => map.Name).ShouldBe(["m1a"]);
        roots["root1"].Operations.Single(op => op.Name == "op1b").Mappings.ShouldBeEmpty();
        roots["root2"].Operations.Single().Mappings.Select(map => map.Name).ShouldBe(["m2"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARowKeepsItsIdentityFromTheDomainRow()
    {
        var store = new SetStore().WithRoots(2);

        var result = await store.Domain.Get(Levels, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Select(root => root.Id).OrderBy(id => id).ShouldBe(store.DomainIds.OrderBy(id => id));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Performance")]
    public async Task ASetLargerThanOneStatementCarriesIsReadInChunks()
    {
        var count = SmallKeyChunkSize + 1;
        var store = new SetStore(SmallKeyChunkSize).WithRoots(count);
        for (var i = 0; i < count; i++)
            store.Gateway.Add("TestOp", Row(1000 + i, "TestRootRowId", 11 + i, $"op{i}"));

        var result = await store.Domain.Get(Levels, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Count.ShouldBe(count);
        result.Value.Cast<TestRootConfiguration>().ShouldAllBe(root => root.Operations.Count == 1);
        store.Gateway.Reads("TestRoot").ShouldBe(2);
        store.Gateway.Reads("TestOp").ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public async Task NoLevelsReadsNoChildTable()
    {
        var store = new SetStore().WithRoots(2);
        store.Gateway.Add("TestOp", Row(21, "TestRootRowId", 11, "op1"));

        var result = await store.Domain.Get(ConfigurationLevels.RecordOnly, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Cast<TestRootConfiguration>().ShouldAllBe(root => root.Operations.Count == 0);
        store.Gateway.Reads("TestOp").ShouldBe(0);
        store.Gateway.Reads("TestMap").ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AChildOfARowNotInTheSetFailsTheReadAndIsNotDropped()
    {
        var store = new SetStore().WithRoots(2);
        store.Gateway.Add("TestOp", Row(21, "TestRootRowId", 999, "stray"));
        store.Gateway.IgnoreFilters = true;

        var result = await store.Domain.Get(Levels, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Cascade")]
    public async Task ADomainRowWithNoCurrentImplementationIsLeftOutOfTheList()
    {
        var store = new SetStore().WithRoots(2);
        store.Gateway.Remove("TestRoot", 12);

        var result = await store.Domain.Get(Levels, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Select(root => root.Name).ShouldBe(["root1"]);
    }

    private static Dictionary<string, object?> Row(int rowId, string ownerColumn, int ownerRowId, string name)
        => new(StringComparer.Ordinal) { ["RowId"] = rowId, [ownerColumn] = ownerRowId, ["Id"] = Guid.NewGuid(), ["Name"] = name };

    /// <summary>A domain provider over a store that holds roots, their operations and the operations' mappings, and counts what is read.</summary>
    private sealed class SetStore
    {
        public SetStore(int keyChunkSize = 1000)
        {
            Gateway = new CountingGateway();
            var gateways = new Gateways(Gateway, keyChunkSize);
            Domain = new TestRootDomainProvider(gateways);
            Domain.Register("Default", new TestRootImplementationProvider(gateways));
        }

        public CountingGateway Gateway { get; }

        public TestRootDomainProvider Domain { get; }

        public List<Guid> DomainIds { get; } = [];

        /// <summary>Holds <paramref name="count"/> roots, each with its domain row and implementation row and no children.</summary>
        public SetStore WithRoots(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var id = Guid.NewGuid();
                DomainIds.Add(id);
                var name = $"root{i + 1}";
                Gateway.AddDomain(
                    new Dictionary<string, object?>(StringComparer.Ordinal) { ["RowId"] = 1 + i, ["Id"] = id },
                    new DomainConfiguration { Id = id, Name = name, Domain = "TestRoot", Implementation = "Default" });
                Gateway.Add("TestRoot", new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["RowId"] = 11 + i, ["TestRootDomainRowId"] = 1 + i, ["Id"] = Guid.NewGuid(), ["Name"] = name,
                });
            }

            return this;
        }
    }

    private sealed class Gateways : IConfigurationGatewayProvider
    {
        private readonly IConfigurationGateway _gateway;

        public Gateways(IConfigurationGateway gateway, int keyChunkSize)
        {
            _gateway = gateway;
            KeyChunkSize = keyChunkSize;
        }

        public int KeyChunkSize { get; }

        public IGenericResult<IConfigurationGateway> Get(string connectionName) => GenericResult<IConfigurationGateway>.Success(_gateway);

        public IGenericResult Register(IConfigurationGateway gateway) => GenericResult.Success();
    }

    /// <summary>
    /// A gateway that holds rows by container, answers a read by the <c>IN</c> condition it carries (as a store does) and counts the statements that read each container.
    /// </summary>
    private sealed class CountingGateway : IConfigurationGateway
    {
        private readonly Dictionary<string, List<Dictionary<string, object?>>> _rows = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _reads = new(StringComparer.Ordinal);
        private readonly List<DomainConfiguration> _domainRecords = [];

        public CountingGateway() => DataStores = [Tree()];

        /// <summary>Answers every read with every row it holds, ignoring the condition: a store that returned rows it was not asked for.</summary>
        public bool IgnoreFilters { get; set; }

        public string ConnectionName => "PlatformConfiguration";

        public IReadOnlyList<IDataStore> DataStores { get; }

        public int Reads(string container) => _reads.GetValueOrDefault(container);

        public void Add(string container, params Dictionary<string, object?>[] rows)
        {
            if (!_rows.TryGetValue(container, out var held))
                _rows[container] = held = [];

            held.AddRange(rows);
        }

        public void AddDomain(Dictionary<string, object?> row, DomainConfiguration record)
        {
            Add("TestRootDomain", row);
            _domainRecords.Add(record);
        }

        public void Remove(string container, int rowId)
            => _rows[container].RemoveAll(row => Equals(row["RowId"], rowId));

        public void InvalidateCachedResults(DataStoreTarget target)
        {
        }

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        {
            if (typeof(T) == typeof(IEnumerable<DomainConfiguration>))
                return Task.FromResult(GenericResult<T>.Success((T)(object)_domainRecords.ToList()));

            if (typeof(T) == typeof(IEnumerable<Dictionary<string, object?>>))
            {
                _reads[target.Container] = _reads.GetValueOrDefault(target.Container) + 1;
                var held = _rows.GetValueOrDefault(target.Container) ?? [];
                var selected = IgnoreFilters ? held : held.Where(row => Selected(command, row)).ToList();
                return Task.FromResult(GenericResult<T>.Success((T)(object)selected.Select(row => new Dictionary<string, object?>(row, StringComparer.Ordinal)).ToList()));
            }

            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return Task.FromResult(GenericResult<T>.Success((T)(object)Array.CreateInstance(typeof(T).GetGenericArguments()[0], 0)));

            return Task.FromResult(GenericResult<T>.Success(default!));
        }

        // A row is selected when every IN condition names a column it holds the value of, as a store compares them.
        private static bool Selected(IDataCommand command, Dictionary<string, object?> row)
        {
            if (command is not IQueryCommand { Filter.Root: { } root })
                return true;

            return Conditions(root)
                .Where(c => string.Equals(c.Operator.SqlOperator, "IN", StringComparison.Ordinal) && c.Value is IEnumerable and not string)
                .All(c => row.TryGetValue(c.PropertyName, out var value)
                          && ((IEnumerable)c.Value!).Cast<object?>().Any(wanted => string.Equals(Text(wanted), Text(value), StringComparison.Ordinal)));
        }

        private static string Text(object? value) => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;

        private static IEnumerable<FilterCondition> Conditions(IFilterNode node)
        {
            if (node is FilterCondition condition)
            {
                yield return condition;
                yield break;
            }

            if (node is FilterGroup group)
            {
                foreach (var nested in group.Nodes.SelectMany(Conditions))
                    yield return nested;
            }
        }

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, CancellationToken cancellationToken = default)
            => Execute<T>(command, default(DataStoreTarget)!, cancellationToken);

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken cancellationToken = default)
            => Execute<T>(command, target, cancellationToken);

        public Task<IGenericResult> Execute(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => Task.FromResult<IGenericResult>(GenericResult.Success());

        public Task<IGenericResult<IEnumerable<object>>> Execute(IDataCommand command, DataStoreTarget target, Type rowType, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<IEnumerable<object>>.Failure(new GenericMessage("A read by row type is the per-row path; the set read does not take it")));

        public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataSetTarget target, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<T>.Failure(new GenericMessage("DataSet routing not supported in CountingGateway")));

        public Task<IGenericResult<Fdw.Data.RowSources.Abstractions.IRecordSource<Fdw.Data.RowSources.Abstractions.DataRecord>>> OpenRecordSource(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IGenericResult<IDataGatewayTransaction>> BeginTransaction(string connectionName, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IGenericResult<T>> Execute<T>(IVectorizedDataCommand<T> command, DataStoreTarget target, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<T>.Failure(new GenericMessage("Vectorized commands not supported in CountingGateway")));

        string IPlatformService.Id => "CountingGateway";

        string IPlatformService.ServiceType => "DataGateway";

        bool IPlatformService.IsAvailable => true;

        Task<IGenericResult<T>> IGenericService.Execute<T>(IGenericCommand command, CancellationToken cancellationToken)
            => throw new NotSupportedException("This fixture routes by target, never by a bare command.");

        Task<IGenericResult> IGenericService.Execute(IGenericCommand command, CancellationToken cancellationToken)
            => throw new NotSupportedException("This fixture routes by target, never by a bare command.");

        // The ownership tree: a domain row owns its implementation row, which owns operations, which own mappings. Each owner is referenced by the owned
        // container's Foreign key to its Physical key (RowId).
        private static IDataStore Tree()
        {
            var containers = new List<IDataContainer>();
            var path = new Mock<IDataNodePath>();
            path.Setup(p => p.Name).Returns("pipe");
            path.Setup(p => p.Containers).Returns(containers);
            path.Setup(p => p.Container(It.IsAny<string>())).Returns((string name) =>
                containers.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.Ordinal)) is { } found
                    ? GenericResult<IDataContainer>.Success(found)
                    : GenericResult<IDataContainer>.Failure(new GenericMessage($"No container '{name}'")));

            IDataContainer Container(string name, params (string Column, IDataContainer Owned)[] owns)
            {
                var container = new Mock<IDataContainer>();
                container.Setup(c => c.Name).Returns(name);
                container.Setup(c => c.Parent).Returns(path.Object);
                container.Setup(c => c.Keys).Returns([Key("Physical", "RowId"), Key("Logical", "Id")]);
                IReadOnlyList<ReferencingKeyBinding> referencing = [.. owns.Select(o => new ReferencingKeyBinding(Key("Foreign", o.Column, Referenced()), o.Owned))];
                container.Setup(c => c.ReferencingKeys).Returns(GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success(referencing));
                containers.Add(container.Object);
                return container.Object;
            }

            var map = Container("TestMap");
            var op = Container("TestOp", ("TestOpRowId", map));
            var root = Container("TestRoot", ("TestRootRowId", op));
            Container("TestRootDomain", ("TestRootDomainRowId", root));

            var store = new Mock<IDataStore>();
            store.Setup(s => s.Name).Returns("PlatformConfiguration");
            store.Setup(s => s.Paths).Returns([path.Object]);
            store.Setup(s => s.Path(It.IsAny<string>())).Returns(GenericResult<IDataNodePath>.Success(path.Object));
            return store.Object;
        }

        private static IDataField Referenced()
        {
            var field = new Mock<IDataField>();
            field.Setup(f => f.Name).Returns("RowId");
            return field.Object;
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
    }
}
