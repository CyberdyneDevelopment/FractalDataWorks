using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Runtime;
using Fdw.Services.Data.Transformations;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// A Compound data set read a page at a time in its one store (FDW-4982, FDW-4991): its filter and order name each field's own container,
/// a page follows the key order, and paging by offset or by keyset returns every joined row once.
/// </summary>
/// <remarks>
/// The store is a fake that answers the joined query the way SQL would: it filters, orders and pages rows keyed by the query's
/// container-qualified names and returns each column under its projected alias. What it is sent is recorded.
/// </remarks>
public sealed class CompoundDataSetPagingTests
{
    private static readonly Guid ConnectionId = Guid.Parse("22222222-0000-0000-0000-000000000002");

    [Fact]
    [Trait("Category", "DataSets.FDW4991")]
    public async Task APagedReadQualifiesEachFieldWithItsSourceAndPagesInKeyOrder()
    {
        var (context, store, stores) = Build(KeyedFields());
        var command = new QueryCommand<Dictionary<string, object?>>
        {
            Filter = new FilterExpression
            {
                Root = new FilterGroup
                {
                    Operator = LogicalOperator.And,
                    Nodes =
                    [
                        Condition("Status", "Equal", "open"),
                        Condition("CustomerName", "Equal", "Ada"),
                    ],
                },
            },
            Paging = new PagingExpression { Skip = 0, Take = 3 },
        };

        var result = await new CompoundDataSetType().Execute<IEnumerable<Dictionary<string, object?>>>(context, command, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var sent = store.Sent.ShouldHaveSingleItem();
        var conditions = ((FilterGroup)sent.Filter.ShouldNotBeNull().Root.ShouldNotBeNull()).Nodes.Cast<IFilterCondition>().ToList();
        conditions.Select(c => c.PropertyName).ShouldBe(["Order.status", "Customer.name"]);
        sent.Ordering.ShouldNotBeNull().OrderedFields.Select(f => f.PropertyName).ShouldBe(["Order.order_id"]);
        sent.Paging.ShouldNotBeNull().Take.ShouldBe(3);

        // The joined columns come back under the data set's names.
        var rows = result.Value.ShouldNotBeNull().ToList();
        rows.Select(r => r["OrderId"]).ShouldBe([1, 3]);
        rows.ShouldAllBe(r => Equals(r["CustomerName"], "Ada"));

        // The join target is resolved with the primary container, so the translator can find it in the store (FDW-4328).
        stores.Verify(s => s.Get("Store", "dbo", "Order", It.Is<IReadOnlyCollection<string>>(c => c.SequenceEqual(new[] { "Customer" })), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "DataSets.FDW4991")]
    public async Task ACallersOrderEndsOnTheKeyFieldsSoEqualValuesStillHaveOnePlace()
    {
        var (context, store, _) = Build(KeyedFields());
        var command = new QueryCommand<Dictionary<string, object?>>
        {
            Ordering = new OrderingExpression { OrderedFields = [new OrderedField { PropertyName = "CustomerName", Direction = SortDirections.Descending }] },
            Paging = new PagingExpression { Skip = 0, Take = 2 },
        };

        var result = await new CompoundDataSetType().Execute<IEnumerable<Dictionary<string, object?>>>(context, command, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var ordered = store.Sent.ShouldHaveSingleItem().Ordering.ShouldNotBeNull().OrderedFields;
        ordered.Select(f => f.PropertyName).ShouldBe(["Customer.name", "Order.order_id"]);
        ordered[0].Direction.IsAscending.ShouldBeFalse();
        ordered[1].Direction.IsAscending.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "DataSets.FDW4991")]
    public async Task OffsetPagesReturnEveryJoinedRowOnceInKeyOrder()
    {
        var (context, _, _) = Build(KeyedFields());
        var seen = new List<object?>();
        for (var skip = 0; ; skip += 2)
        {
            var page = await new CompoundDataSetType().Execute<IEnumerable<Dictionary<string, object?>>>(
                context,
                new QueryCommand<Dictionary<string, object?>> { Paging = new PagingExpression { Skip = skip, Take = 2 } },
                TestContext.Current.CancellationToken);
            page.IsSuccess.ShouldBeTrue(page.CurrentMessage);
            var rows = page.Value.ShouldNotBeNull().ToList();
            seen.AddRange(rows.Select(r => r["OrderId"]));
            if (rows.Count < 2)
                break;
        }

        seen.ShouldBe([1, 2, 3, 4, 5]);
    }

    [Fact]
    [Trait("Category", "DataSets.FDW4991")]
    public async Task KeysetPagesOverTheJoinReturnEveryRowOnce()
    {
        var (context, store, _) = Build(KeyedFields());
        var kind = new CompoundDataSetType();
        var schema = new RecordSchema([.. KeyedFields().Select(f => (Fdw.Data.Abstractions.IDataField)new DataSetRuntimeField(f))]);
        await using var source = new KeysetRecordSource(
            schema,
            (command, ct) => kind.Execute<IEnumerable<Dictionary<string, object?>>>(context, command, ct),
            new QueryCommand<Dictionary<string, object?>>(),
            ["OrderId"],
            includeKeys: false,
            pageSize: 2,
            "data set 'OrderCustomers'");

        var ids = new List<object?>();
        await foreach (var record in source.Read(TestContext.Current.CancellationToken))
        {
            record.IsSuccess.ShouldBeTrue(record.CurrentMessage);
            ids.Add(record.Value["OrderId"]);
        }

        ids.ShouldBe([1, 2, 3, 4, 5]);
        // Every page after the first asks for the rows after the last key, in the key's own container.
        store.Sent.Skip(1).ShouldAllBe(q => q.Filter != null && q.Filter.Root != null);
        ((IFilterCondition)store.Sent[1].Filter!.Root!).PropertyName.ShouldBe("Order.order_id");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW4991")]
    public async Task AFieldNoSourceHoldsIsRefusedAndTheStoreIsNotAsked()
    {
        var (context, store, _) = Build(KeyedFields());
        var command = new QueryCommand<Dictionary<string, object?>>
        {
            Filter = new FilterExpression { Root = Condition("Nope", "Equal", 1) },
        };

        var result = await new CompoundDataSetType().Execute<IEnumerable<Dictionary<string, object?>>>(context, command, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Nope");
        store.Sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Category", "DataSets.FDW4991")]
    public async Task APageOfACompoundSetThatDeclaresNoKeyAndStatesNoOrderIsRefused()
    {
        var (context, store, _) = Build([.. KeyedFields().Select(f => new DataSetFieldConfiguration { Name = f.Name, Ordinal = f.Ordinal })]);

        var result = await new CompoundDataSetType().Execute<IEnumerable<Dictionary<string, object?>>>(
            context,
            new QueryCommand<Dictionary<string, object?>> { Paging = new PagingExpression { Skip = 0, Take = 2 } },
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("key");
        store.Sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Category", "DataSets.FDW4991")]
    public void TheReadGateLetsACompoundSetPageAndStillRefusesToGroupIt()
    {
        var compound = new CompoundDataSetConfiguration { Name = "OrderCustomers", Implementation = "Compound" };

        DataSetReadGate.Pageable(compound, NullLogger.Instance).IsSuccess.ShouldBeTrue();
        DataSetReadGate.Groupable(compound, NullLogger.Instance).IsFailure.ShouldBeTrue();
    }

    [Theory]
    [Trait("Category", "DataSets.FDW5024")]
    [InlineData("Left", "CustomerName")]
    [InlineData("Right", "OrderId")]
    [InlineData("Full", "OrderId")]
    [InlineData("Full", "CustomerName")]
    public async Task APageKeyedOnASourceAnOuterJoinMayLeaveEmptyIsRefusedNamingTheKey(string joinType, string key)
    {
        var (context, store, _) = Build(KeyedOn(key), joinType);

        var result = await new CompoundDataSetType().Execute<IEnumerable<Dictionary<string, object?>>>(
            context,
            new QueryCommand<Dictionary<string, object?>> { Paging = new PagingExpression { Skip = 0, Take = 2 } },
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain(key);
        store.Sent.ShouldBeEmpty();
    }

    [Theory]
    [Trait("Category", "DataSets.FDW5024")]
    [InlineData("Inner", "CustomerName")]
    [InlineData("Left", "OrderId")]
    [InlineData("Right", "CustomerName")]
    public async Task APageKeyedOnASourceEveryJoinedRowHasIsRead(string joinType, string key)
    {
        var (context, store, _) = Build(KeyedOn(key), joinType);

        var result = await new CompoundDataSetType().Execute<IEnumerable<Dictionary<string, object?>>>(
            context,
            new QueryCommand<Dictionary<string, object?>> { Paging = new PagingExpression { Skip = 0, Take = 2 } },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        store.Sent.ShouldHaveSingleItem();
    }

    [Fact]
    [Trait("Category", "DataSets.FDW5024")]
    public async Task AnUnpagedReadOfAnOuterJoinKeyedOnItsOptionalSideIsRead()
    {
        var (context, store, _) = Build(KeyedOn("CustomerName"), "Left");

        var result = await new CompoundDataSetType().Execute<IEnumerable<Dictionary<string, object?>>>(
            context, new QueryCommand<Dictionary<string, object?>>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        store.Sent.ShouldHaveSingleItem();
    }

    [Fact]
    [Trait("Category", "DataSets.FDW5022")]
    public async Task AStoredFieldNoSourceMapsIsRefusedNotReadFromThePrimarySource()
    {
        var (context, store, _) = Build([.. KeyedFields(), new DataSetFieldConfiguration { Name = "Region", Ordinal = 3 }]);

        var result = await new CompoundDataSetType().Execute<IEnumerable<Dictionary<string, object?>>>(
            context, new QueryCommand<Dictionary<string, object?>>(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code!.Code.ShouldBe("DATASETS-20018");
        store.Sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Category", "DataSets.FDW5022")]
    public async Task AJoinColumnItsSourceDoesNotMapIsRefusedNotSentUnderItsLogicalName()
    {
        var (context, store, _) = Build(KeyedFields(), leftJoinField: "CustomerKey");

        var result = await new CompoundDataSetType().Execute<IEnumerable<Dictionary<string, object?>>>(
            context, new QueryCommand<Dictionary<string, object?>>(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code!.Code.ShouldBe("DATASETS-20020");
        store.Sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Category", "DataSets.FDW4997")]
    public async Task TheDataSetsOwnFilterIsAndedWithTheCallersAndOnlyMatchingJoinedRowsComeBack()
    {
        var (context, store, _) = Build(
            KeyedFields(),
            filters: [new DataSetFilterConditionConfiguration { FieldName = "Status", Operator = "Equal", Value = "open", DataType = "String", Ordinal = 0 }]);
        var command = new QueryCommand<Dictionary<string, object?>>
        {
            Filter = new FilterExpression { Root = Condition("CustomerName", "Equal", "Ada") },
            Paging = new PagingExpression { Skip = 0, Take = 10 },
        };

        var result = await new CompoundDataSetType().Execute<IEnumerable<Dictionary<string, object?>>>(context, command, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var group = store.Sent.ShouldHaveSingleItem().Filter.ShouldNotBeNull().Root.ShouldBeOfType<FilterGroup>();
        group.Nodes.Cast<IFilterCondition>().Select(c => c.PropertyName).ShouldBe(["Order.status", "Customer.name"]);
        // Order 5 is Ada's but closed: the data set's own filter keeps it out.
        result.Value.ShouldNotBeNull().Select(r => r["OrderId"]).ShouldBe([1, 3]);
    }

    [Fact]
    [Trait("Category", "DataSets.FDW4997")]
    public async Task KeysetPagesCarryTheDataSetsOwnFilterOnEveryPage()
    {
        var (context, store, _) = Build(
            KeyedFields(),
            filters: [new DataSetFilterConditionConfiguration { FieldName = "Status", Operator = "Equal", Value = "open", DataType = "String", Ordinal = 0 }]);
        var kind = new CompoundDataSetType();
        await using var source = new KeysetRecordSource(
            new RecordSchema([.. KeyedFields().Select(f => (Fdw.Data.Abstractions.IDataField)new DataSetRuntimeField(f))]),
            (command, ct) => kind.Execute<IEnumerable<Dictionary<string, object?>>>(context, command, ct),
            new QueryCommand<Dictionary<string, object?>>(),
            ["OrderId"],
            includeKeys: false,
            pageSize: 2,
            "data set 'OrderCustomers'");

        var ids = new List<object?>();
        await foreach (var record in source.Read(TestContext.Current.CancellationToken))
        {
            record.IsSuccess.ShouldBeTrue(record.CurrentMessage);
            ids.Add(record.Value["OrderId"]);
        }

        ids.ShouldBe([1, 3, 4]);
        store.Sent.Count.ShouldBe(2);
    }

    private static List<DataSetFieldConfiguration> KeyedOn(string key) =>
        [.. KeyedFields().Select(f => new DataSetFieldConfiguration { Name = f.Name, Ordinal = f.Ordinal, IsKey = string.Equals(f.Name, key, StringComparison.Ordinal) })];

    private static List<DataSetFieldConfiguration> KeyedFields() =>
    [
        new() { Name = "OrderId", Ordinal = 0, IsKey = true },
        new() { Name = "Status", Ordinal = 1 },
        new() { Name = "CustomerName", Ordinal = 2 },
    ];

    private static FilterCondition Condition(string field, string operatorName, object? value) =>
        new() { PropertyName = field, Operator = FilterOperators.ByName(operatorName), Value = value };

    private static DataSetSourceConfiguration Source(string name, string container, int priority, params (string Logical, string Physical)[] mappings) => new()
    {
        Name = name,
        SourceName = name,
        DataStoreName = "Store",
        PathValue = "dbo",
        ContainerName = container,
        Priority = priority,
        Mappings = [.. mappings.Select(m => new DataSetFieldMappingConfiguration
        {
            Id = Guid.CreateVersion7(), Name = m.Logical, LogicalFieldName = m.Logical, PhysicalFieldName = m.Physical,
        })],
    };

    private static (DataSetExecutionContext Context, JoinedStore Store, Mock<IDataStoreProvider> Stores) Build(List<DataSetFieldConfiguration> fields, string joinType = "Inner", string leftJoinField = "CustomerCode", IEnumerable<DataSetFilterConditionConfiguration>? filters = null)
    {
        var container = new Mock<IDataContainer> { DefaultValue = DefaultValue.Mock };
        container.Setup(c => c.Parent.Store.ConnectionId).Returns(ConnectionId);
        var stores = new Mock<IDataStoreProvider>();
        stores.Setup(s => s.Get("Store", "dbo", "Order", It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataContainer>.Success(container.Object));

        var store = new JoinedStore();
        var connection = new Mock<IDataConnection>();
        connection.Setup(c => c.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDataCommand command, IDataContainer _, CancellationToken _) =>
                GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(store.Answer((IQueryCommand)command)));
        var connections = new Mock<IDataConnectionProvider>();
        connections.Setup(c => c.Get(ConnectionId, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IDataConnection>.Success(connection.Object));

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

        var config = new CompoundDataSetConfiguration
        {
            Name = "OrderCustomers",
            Implementation = "Compound",
            Sources =
            [
                Source("Orders", "Order", 1, ("OrderId", "order_id"), ("Status", "status"), ("CustomerCode", "customer_code")),
                Source("Customers", "Customer", 2, ("CustomerName", "name"), ("Code", "code")),
            ],
            Joins = [new DataSetJoinConfiguration { LeftSourceName = "Orders", LeftFieldName = leftJoinField, RightSourceName = "Customers", RightFieldName = "Code", JoinType = joinType, Ordinal = 1 }],
            Fields = fields,
            Filters = [.. filters ?? []],
        };
        var context = new DataSetExecutionContext(
            config, connections.Object, stores.Object,
            new PredicatePushdownAnalyzer(NullLogger<PredicatePushdownAnalyzer>.Instance), resolver, NullLogger.Instance);
        return (context, store, stores);
    }

    // Orders joined to Customers, as the store holds the join's rows: each keyed by its container-qualified column.
    private sealed class JoinedStore
    {
        private readonly List<Dictionary<string, object?>> _rows =
        [
            Row(4, "open", "Grace"),
            Row(1, "open", "Ada"),
            Row(5, "closed", "Ada"),
            Row(3, "open", "Ada"),
            Row(2, "closed", "Grace"),
        ];

        public List<IQueryCommand> Sent { get; } = [];

        public IEnumerable<Dictionary<string, object?>> Answer(IQueryCommand query)
        {
            Sent.Add(query);
            var rows = _rows.Where(row => query.Filter?.Root is not { } root || Matches(root, row));
            if (query.Ordering is { OrderedFields.Count: > 0 } ordering)
            {
                var sorted = Sort(rows, ordering.OrderedFields[0]);
                sorted = ordering.OrderedFields.Skip(1).Aggregate(sorted, ThenSort);
                rows = sorted;
            }

            if (query.Paging is { } paging)
                rows = rows.Skip(paging.Skip).Take(paging.Take ?? int.MaxValue);

            var projected = query.Projection.ShouldNotBeNull().Fields;
            return [.. rows.Select(row => projected.ToDictionary(f => f.Alias!, f => row[$"{f.SourceContainer}.{f.PropertyName}"], StringComparer.Ordinal))];
        }

        private static Dictionary<string, object?> Row(int orderId, string status, string customer) => new(StringComparer.Ordinal)
        {
            ["Order.order_id"] = orderId,
            ["Order.status"] = status,
            ["Order.customer_code"] = customer[..1],
            ["Customer.code"] = customer[..1],
            ["Customer.name"] = customer,
        };

        private static IOrderedEnumerable<Dictionary<string, object?>> Sort(IEnumerable<Dictionary<string, object?>> rows, IOrderedField field) =>
            field.Direction.IsAscending
                ? rows.OrderBy(row => row[field.PropertyName], Comparer<object?>.Default)
                : rows.OrderByDescending(row => row[field.PropertyName], Comparer<object?>.Default);

        private static IOrderedEnumerable<Dictionary<string, object?>> ThenSort(IOrderedEnumerable<Dictionary<string, object?>> rows, IOrderedField field) =>
            field.Direction.IsAscending
                ? rows.ThenBy(row => row[field.PropertyName], Comparer<object?>.Default)
                : rows.ThenByDescending(row => row[field.PropertyName], Comparer<object?>.Default);

        private static bool Matches(IFilterNode node, Dictionary<string, object?> row) => node switch
        {
            IFilterCondition condition => Compare(condition, row),
            FilterGroup group when ReferenceEquals(group.Operator, LogicalOperator.And) => group.Nodes.All(child => Matches(child, row)),
            FilterGroup group => group.Nodes.Any(child => Matches(child, row)),
            _ => throw new InvalidOperationException("an unknown filter node"),
        };

        private static bool Compare(IFilterCondition condition, Dictionary<string, object?> row)
        {
            var order = Comparer.Default.Compare(row[condition.PropertyName], condition.Value);
            return condition.Operator.Name switch
            {
                "Equal" => order == 0,
                "GreaterThan" => order > 0,
                _ => throw new InvalidOperationException($"the fake store does not compare with {condition.Operator.Name}"),
            };
        }
    }
}
