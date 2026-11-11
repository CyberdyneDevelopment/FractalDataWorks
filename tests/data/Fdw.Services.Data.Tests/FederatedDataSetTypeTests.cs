using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataContainers.Abstractions;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Transformations;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;
using IDataField = Fdw.Data.Abstractions.IDataField;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// What a Federated data set joins on and answers with (FDW-3629).
/// </summary>
/// <remarks>
/// Why these exist: the strategy used a source's field mappings only to translate the filter, then joined and returned the records in
/// the store's names. A join on a logical name read null on every record and matched nothing, with no error. Each source here is a
/// mocked record-capable connection whose records carry the store's column names.
/// </remarks>
public sealed class FederatedDataSetTypeTests
{
    private static readonly Guid OrdersConnection = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid CustomersConnection = Guid.Parse("11111111-0000-0000-0000-000000000002");

    private sealed class Records(RecordSchema schema, IReadOnlyList<object?[]> rows) : IRecordSource<DataRecord>
    {
        public RecordSchema Schema => schema;

        public IEnumerable<IGenericResult<DataRecord>> Read() =>
            rows.Select(row => GenericResult<DataRecord>.Success(new DataRecord(schema, row)));

        public async IAsyncEnumerable<IGenericResult<DataRecord>> Read([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var row in rows)
            {
                await Task.Yield();
                yield return GenericResult<DataRecord>.Success(new DataRecord(schema, row));
            }
        }

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private static RecordSchema Schema(params string[] names) =>
        new([.. names.Select(name => Mock.Of<IDataField>(field => field.Name == name))]);

    private static DataSetSourceConfiguration Source(string name, string container, params (string Logical, string Physical)[] mappings) => new()
    {
        Name = name,
        SourceName = name,
        DataStoreName = "Store",
        PathValue = "dbo",
        ContainerName = container,
        Mappings = [.. mappings.Select(m => new DataSetFieldMappingConfiguration
        {
            Id = Guid.CreateVersion7(), Name = m.Logical, LogicalFieldName = m.Logical, PhysicalFieldName = m.Physical,
        })],
    };

    private static DataSetExecutionContext Context(
        IReadOnlyList<DataSetSourceConfiguration> sources, DataSetJoinConfiguration join, Records orders, Records customers)
    {
        var stores = new Mock<IDataStoreProvider>();
        Store(stores, "Order", OrdersConnection);
        Store(stores, "Customer", CustomersConnection);

        var connections = new Mock<IDataConnectionProvider>();
        Connection(connections, OrdersConnection, orders);
        Connection(connections, CustomersConnection, customers);

        // Every mapping has no transformation attached: the resolver reads the attachments and finds none.
        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.Execute<IEnumerable<FieldMappingTransformationConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<FieldMappingTransformationConfiguration>>.Success([]));
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(g => g.Get(It.IsAny<string>())).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
        var resolver = new FieldMappingTransformationResolver(
            gateways.Object,
            new TransformationConfigurationProvider(
                NullLogger<TransformationConfigurationProvider>.Instance, gateways.Object, "PlatformConfiguration", "transform", "Transformation"));

        var config = new FederatedDataSetConfiguration
        {
            Name = "CustomerOrders",
            Implementation = "Federated",
            FederationStrategy = "Sequential",
            Sources = [.. sources],
            Joins = [join],
        };

        return new DataSetExecutionContext(
            config,
            connections.Object,
            stores.Object,
            new PredicatePushdownAnalyzer(NullLogger<PredicatePushdownAnalyzer>.Instance),
            resolver,
            NullLogger.Instance);
    }

    private static void Store(Mock<IDataStoreProvider> stores, string container, Guid connectionId)
    {
        var found = new Mock<IDataContainer> { DefaultValue = DefaultValue.Mock };
        found.Setup(c => c.Parent.Store.ConnectionId).Returns(connectionId);
        stores.Setup(s => s.Get("Store", "dbo", container, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataContainer>.Success(found.Object));
    }

    private static void Connection(Mock<IDataConnectionProvider> connections, Guid id, Records records)
    {
        var connection = new Mock<IDataConnection>();
        connection.As<IRecordSourceConnection>()
            .Setup(c => c.OpenRecordSource(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRecordSource<DataRecord>>.Success(records));
        connections.Setup(c => c.Get(id, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IDataConnection>.Success(connection.Object));
    }

    private static DataSetJoinConfiguration Join(string type, string left = "CustomerId", string right = "CustomerId") => new()
    {
        LeftSourceName = "Orders", LeftFieldName = left, RightSourceName = "Customers", RightFieldName = right, JoinType = type, Ordinal = 1,
    };

    private static IReadOnlyList<DataSetSourceConfiguration> MappedSources() =>
    [
        Source("Orders", "Order", ("CustomerId", "cust_id"), ("OrderTotal", "total")),
        Source("Customers", "Customer", ("CustomerId", "id"), ("CustomerName", "name")),
    ];

    private static Records Orders() => new(Schema("cust_id", "total"), [[7, 10m], [8, 25m]]);

    private static Records Customers() => new(Schema("id", "name"), [[7, "Acme"]]);

    private static Task<IGenericResult<IEnumerable<DataRecord>>> Run(DataSetExecutionContext context) =>
        new FederatedDataSetType().Execute<IEnumerable<DataRecord>>(context, new QueryCommand<DataRecord>(), TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Category", "DataSets.FDW3629")]
    public async Task SourcesWithDifferentPhysicalNamesJoinOnTheirLogicalNamesAndAnswerInThem()
    {
        var result = await Run(Context(MappedSources(), Join("Inner"), Orders(), Customers()));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var row = result.Value.ShouldNotBeNull().ShouldHaveSingleItem();
        row["CustomerName"].ShouldBe("Acme");
        row["OrderTotal"].ShouldBe(10m);
        row.Schema.GetFieldOrdinal("cust_id").ShouldBe(-1, "the result speaks the data set's names, not the store's");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW3629")]
    public async Task ALeftJoinKeepsTheLeftRowsThatMatchNothing()
    {
        var result = await Run(Context(MappedSources(), Join("Left"), Orders(), Customers()));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var rows = result.Value.ShouldNotBeNull().ToList();
        rows.Count.ShouldBe(2);
        rows.Single(r => Equals(r["OrderTotal"], 25m))["CustomerName"].ShouldBeNull();
    }

    [Fact]
    [Trait("Category", "DataSets.FDW3629")]
    public async Task AKeyBothSidesNameTheSameIsKeptOnceSoALeftRowThatMatchesNothingKeepsItsKey()
    {
        var result = await Run(Context(MappedSources(), Join("Left"), Orders(), Customers()));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var unmatched = result.Value.ShouldNotBeNull().Single(r => Equals(r["OrderTotal"], 25m));
        unmatched["CustomerId"].ShouldBe(8, "the right side's null must not shadow the left row's own key");
        unmatched.Schema.Fields.Count(f => string.Equals(f.Name, "CustomerId", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
    }

    [Fact]
    [Trait("Category", "DataSets.FDW3629")]
    public async Task AJoinOnAFieldItsSourceDoesNotHaveIsRefusedNotAnsweredWithNothing()
    {
        var result = await Run(Context(MappedSources(), Join("Inner", left: "Region"), Orders(), Customers()));

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Region");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW3629")]
    public async Task SourcesWithNoMappingsJoinOnTheirOwnNames()
    {
        var result = await Run(Context(
            [Source("Orders", "Order"), Source("Customers", "Customer")],
            Join("Inner", left: "cust_id", right: "id"),
            Orders(),
            Customers()));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldNotBeNull().ShouldHaveSingleItem()["name"].ShouldBe("Acme");
    }

    // FDW-5049: the whole read filter applies to the joined rows. These sources answer every row whatever they are asked, so what the
    // result keeps is what the data set filtered after the join.
    private static Task<IGenericResult<IEnumerable<DataRecord>>> RunFiltered(DataSetExecutionContext context, IFilterNode root) =>
        new FederatedDataSetType().Execute<IEnumerable<DataRecord>>(
            context, new QueryCommand<DataRecord> { Filter = new FilterExpression { Root = root } }, TestContext.Current.CancellationToken);

    private static FilterCondition Where(string field, string op, object? value) =>
        new() { PropertyName = field, Operator = FilterOperators.ByName(op), Value = value };

    [Fact]
    [Trait("Category", "DataSets.FDW5049")]
    public async Task AConditionOnASourceThatDoesNotFilterIsAppliedToTheJoinedRows()
    {
        var result = await RunFiltered(Context(MappedSources(), Join("Left"), Orders(), Customers()), Where("OrderTotal", "GreaterThan", 15m));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldNotBeNull().ShouldHaveSingleItem()["OrderTotal"].ShouldBe(25m);
    }

    [Fact]
    [Trait("Category", "DataSets.FDW5049")]
    public async Task AConditionOnAFieldBothSourcesMapIsAppliedToTheJoinedRowNotToTheFirstSourceOnly()
    {
        var result = await RunFiltered(Context(MappedSources(), Join("Left"), Orders(), Customers()), Where("CustomerId", "Equal", 8));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var row = result.Value.ShouldNotBeNull().ShouldHaveSingleItem();
        row["OrderTotal"].ShouldBe(25m);
        row["CustomerName"].ShouldBeNull();
    }

    [Fact]
    [Trait("Category", "DataSets.FDW5049")]
    public async Task AConditionOnTheOptionalSideOfALeftJoinIsAppliedToTheJoinedRows()
    {
        var result = await RunFiltered(Context(MappedSources(), Join("Left"), Orders(), Customers()), Where("CustomerName", "IsNull", null));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldNotBeNull().ShouldHaveSingleItem()["OrderTotal"].ShouldBe(25m);
    }

    [Fact]
    [Trait("Category", "DataSets.FDW5049")]
    public async Task AConditionOnAFieldTheJoinedRowsDoNotCarryIsRefused()
    {
        var result = await RunFiltered(Context(MappedSources(), Join("Inner"), Orders(), Customers()), Where("Region", "Equal", "north"));

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("DATA-21202");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Region");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW5049")]
    public async Task AJoinToASourceOutsideTheDataSetIsRefusedBeforeAnySourceIsRead()
    {
        var join = Join("Inner");
        join.RightSourceName = "Suppliers";

        var result = await Run(Context(MappedSources(), join, Orders(), Customers()));

        result.IsFailure.ShouldBeTrue();
        result.Code!.Code.ShouldBe("DATASETS-20021");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW5049")]
    public void OnlyAConditionOneFilteringSourceHoldsOutsideAnOuterJoinsOptionalSideIsPushedDown()
    {
        var sources = new List<DataSetSourceConfiguration>
        {
            Source("Orders", "Order", ("CustomerId", "cust_id"), ("OrderTotal", "total")),
            Source("Customers", "Customer", ("CustomerId", "id"), ("CustomerName", "name")),
        };
        sources.ForEach(source => source.SupportsPredicatePushdown = true);
        var dataSet = new FederatedDataSetConfiguration { Name = "CustomerOrders", Implementation = "Federated", Sources = sources, Joins = [Join("Left")] };
        var mappings = sources.ToDictionary(s => s.SourceName, s => s.FieldMappings, StringComparer.OrdinalIgnoreCase);
        var filter = new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.And,
                Nodes = [Where("OrderTotal", "GreaterThan", 15m), Where("CustomerId", "Equal", 8), Where("CustomerName", "Equal", "Acme")],
            },
        };

        var decomposed = new PredicatePushdownAnalyzer(NullLogger<PredicatePushdownAnalyzer>.Instance)
            .DecomposeBySource(filter, dataSet, sources, mappings);

        decomposed.IsSuccess.ShouldBeTrue(decomposed.CurrentMessage);
        // OrderTotal is the Orders source's alone; CustomerId is held by both; CustomerName is on the left join's optional side.
        var pushed = decomposed.Value.ShouldNotBeNull().ShouldHaveSingleItem();
        pushed.Key.ShouldBe("Orders");
        pushed.Value.Root.ShouldBeAssignableTo<IFilterCondition>().ShouldNotBeNull().PropertyName.ShouldBe("total");
    }

    // Two customers, one with no order: the right side's row that matches nothing (FDW-5087).
    private static Records TwoCustomers() => new(Schema("id", "name"), [[7, "Acme"], [9, "Zenith"]]);

    [Fact]
    [Trait("Category", "DataSets.FDW5087")]
    public async Task ARightJoinKeepsTheRightRowsThatMatchNothingWithTheirOwnKey()
    {
        var result = await Run(Context(MappedSources(), Join("Right"), Orders(), TwoCustomers()));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var rows = result.Value.ShouldNotBeNull().ToList();
        rows.Count.ShouldBe(2, "the matched pair and the customer with no order; the order with no customer is dropped");
        var unmatched = rows.Single(r => Equals(r["CustomerName"], "Zenith"));
        unmatched["CustomerId"].ShouldBe(9, "the right row's key fills the shared key's place");
        unmatched["OrderTotal"].ShouldBeNull();
        rows.ShouldNotContain(r => Equals(r["OrderTotal"], 25m));
    }

    [Fact]
    [Trait("Category", "DataSets.FDW5087")]
    public async Task AFullJoinKeepsTheRowsOfBothSidesThatMatchNothing()
    {
        var result = await Run(Context(MappedSources(), Join("Full"), Orders(), TwoCustomers()));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var rows = result.Value.ShouldNotBeNull().ToList();
        rows.Select(r => (r["CustomerId"], r["OrderTotal"], r["CustomerName"])).ShouldBe(
            new (object?, object?, object?)[] { (7, 10m, "Acme"), (8, 25m, null), (9, null, "Zenith") }, ignoreOrder: true);
    }

    [Fact]
    [Trait("Category", "DataSets.FDW5087")]
    public async Task ACrossJoinPairsEveryLeftRowWithEveryRightRow()
    {
        var result = await Run(Context(MappedSources(), Join("Cross"), Orders(), TwoCustomers()));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldNotBeNull().Select(r => (r["OrderTotal"], r["CustomerName"])).ShouldBe(
            new (object?, object?)[] { (10m, "Acme"), (10m, "Zenith"), (25m, "Acme"), (25m, "Zenith") }, ignoreOrder: true);
    }

    [Theory]
    [Trait("Category", "DataSets.FDW5087")]
    [Trait("Category", "DataSets.FDW5099")]
    [InlineData("None", "DATASETS-20028")]
    [InlineData("Outer", "DATASETS-20029")]
    [InlineData("", "DATASETS-20030")]
    public async Task AJoinTypeThatDoesNotJoinIsRefusedWithItsOwnCodeNotRunAsInner(string joinType, string code)
    {
        var result = await Run(Context(MappedSources(), Join(joinType), Orders(), TwoCustomers()));

        result.IsFailure.ShouldBeTrue();
        result.Code!.Code.ShouldBe(code);
        result.CurrentMessage.ShouldNotBeNull().ShouldContain(joinType);
    }

    [Theory]
    [Trait("Category", "DataSets.FDW5099")]
    [InlineData("left")]
    [InlineData("LEFT")]
    public async Task AJoinTypeIsTheSameOptionWhateverItsCase(string joinType)
    {
        var result = await Run(Context(MappedSources(), Join(joinType), Orders(), Customers()));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldNotBeNull().Count().ShouldBe(2, "a left join keeps the order with no customer");
    }
}
