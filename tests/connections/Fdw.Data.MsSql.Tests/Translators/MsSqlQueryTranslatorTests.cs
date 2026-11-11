using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Fdw.Results;
using Moq;
using Shouldly;
using Xunit;
using Fdw.Data.MsSql.Tests.TestSupport;

namespace Fdw.Data.MsSql.Tests.Translators;

[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlQueryTranslatorTests
{
    private readonly MsSqlQueryTranslator _sut = new();

    private static Mock<IDataContainer> CreateContainer(
        string name = "Customers",
        string schema = "dbo",
        string database = "",
        IField[]? fields = null,
        string? declaredKey = null)
    {
        var dbPath = new DatabasePath(database, schema, name);
        var containerSchema = new Mock<IContainerSchema>();
        containerSchema.Setup(s => s.Fields).Returns(fields ?? new[] { CreateField("Id").Object });
        containerSchema.Setup(s => s.GetReturnedFields(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns(fields ?? new[] { CreateField("Id").Object });

        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns(name);
        container.As<IStorageContainer>().Setup(c => c.Name).Returns(name);
        container.As<IStorageContainer>().Setup(c => c.Path).Returns(dbPath);
        if (declaredKey is not null)
            container.As<IStorageContainer>().Setup(c => c.Metadata).Returns(new Dictionary<string, object> { ["SurrogateKeyField"] = declaredKey });

        container.Setup(c => c.Schema).Returns(containerSchema.Object);
        container.Setup(c => c.ReferencingKeys)
            .Returns(GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success([]));
        container.Setup(c => c.Keys).Returns(new List<IContainerKey>());

        return container;
    }

    private static Mock<IField> CreateField(
        string name,
        bool isIdentity = false,
        bool isComputed = false)
    {
        var field = new Mock<IField>();
        field.Setup(f => f.FieldType).Returns(TestFieldTypes.Text);
        field.Setup(f => f.Name).Returns(name);
        field.Setup(f => f.IsIdentity).Returns(isIdentity);
        field.Setup(f => f.IsComputed).Returns(isComputed);
        return field;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslatorFailsLoudWhenContainerHasNoFields()
    {
        var container = CreateContainer(fields: []);
        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Joins).Returns([]);
        queryCommand.Setup(q => q.Filter).Returns((IFilterExpression?)null);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateSelectFieldsFromSchemaWhenNoProjection()
    {
        var fields = new[]
        {
            CreateField("Id").Object,
            CreateField("Name").Object,
            CreateField("Email").Object
        };
        var container = CreateContainer(fields: fields);

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Joins).Returns([]);
        queryCommand.Setup(q => q.Filter).Returns((IFilterExpression?)null);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldBe("SELECT [Id], [Name], [Email] FROM [dbo].[Customers]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateTypedBodyJoinReadByParentDurableId()
    {
        var domainConfigurationId = Guid.Parse("8383b1b2-c3d4-5e6f-7a8b-9c0d1e2f3a4b");
        var fields = new[] { CreateField("Id").Object, CreateField("ServerName").Object };
        var container = CreateContainer(name: "MsSqlConnection", schema: "conn", fields: fields);

        // A JOIN target is found by name among the containers of the primary container's store, so
        // the store declares both the child and the parent it joins to.
        var parent = CreateContainer(name: "Connection", schema: "conn");
        var path = new Mock<IDataNodePath>();
        path.Setup(p => p.Containers).Returns([container.Object, parent.Object]);
        var store = new Mock<IDataStore>();
        store.Setup(s => s.Name).Returns("PlatformConfiguration");
        store.Setup(s => s.Paths).Returns([path.Object]);
        path.Setup(p => p.Store).Returns(store.Object);
        container.Setup(c => c.Parent).Returns(path.Object);

        var call = new QueryCommandBuilder<object>("PlatformConfiguration", "conn", "MsSqlConnection")
            .Join("Connection", "ConnectionRowId", "RowId")
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Where("Connection.Id", domainConfigurationId)
            .Build();

        var result = await _sut.Translate((IQueryCommand)call.Command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var sql = result.Value!.CommandText;
        sql.ShouldContain("SELECT [MsSqlConnection].[Id], [MsSqlConnection].[ServerName]");
        sql.ShouldContain("FROM [conn].[MsSqlConnection]");
        sql.ShouldContain("INNER JOIN [conn].[Connection] ON [MsSqlConnection].[ConnectionRowId] = [Connection].[RowId]");
        sql.ShouldContain("[MsSqlConnection].[IsCurrent] = @p0");
        sql.ShouldContain("[MsSqlConnection].[IsDeleted] = @p1");
        sql.ShouldContain("[Connection].[Id] = @p2");
        // The parent's RowId is never selected/projected — it lives only in the join.
        sql.ShouldNotContain("[MsSqlConnection].[RowId]");
    }

    // A Compound data set's read (FDW-4991): its own container, the joined one declared in the same store.
    private static Mock<IDataContainer> CompoundStore()
    {
        var order = CreateContainer(name: "Order", schema: "dbo", fields: [CreateField("order_id").Object, CreateField("status").Object]);
        var customer = CreateContainer(name: "Customer", schema: "dbo");
        var path = new Mock<IDataNodePath>();
        path.Setup(p => p.Containers).Returns([order.Object, customer.Object]);
        var store = new Mock<IDataStore>();
        store.Setup(s => s.Name).Returns("Store");
        store.Setup(s => s.Paths).Returns([path.Object]);
        path.Setup(p => p.Store).Returns(store.Object);
        order.Setup(c => c.Parent).Returns(path.Object);
        return order;
    }

    // A Compound read joining Customer by the given type, with the one join condition or none (FDW-5099).
    private static QueryCommand<object> JoinedRead(string joinType, bool withCondition) => new()
    {
        Joins = [new JoinExpression { TargetContainerName = "Customer", JoinType = joinType, JoinConditions = withCondition ? [("customer_code", "code")] : [] }],
        Projection = new ProjectionExpression
        {
            Fields =
            [
                new ProjectionField { PropertyName = "order_id", Alias = "OrderId", SourceContainer = "Order" },
                new ProjectionField { PropertyName = "name", Alias = "CustomerName", SourceContainer = "Customer" },
            ],
        },
    };

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5099")]
    [InlineData("Inner", "INNER JOIN [dbo].[Customer] ON [Order].[customer_code] = [Customer].[code]")]
    [InlineData("left", "LEFT JOIN [dbo].[Customer] ON [Order].[customer_code] = [Customer].[code]")]
    [InlineData("RIGHT", "RIGHT JOIN [dbo].[Customer] ON [Order].[customer_code] = [Customer].[code]")]
    [InlineData("Full", "FULL OUTER JOIN [dbo].[Customer] ON [Order].[customer_code] = [Customer].[code]")]
    public async Task AJoinIsWrittenByItsJoinTypesOwnKeywordNotByTheTextItCarries(string joinType, string clause)
    {
        var result = await _sut.Translate(JoinedRead(joinType, withCondition: true), CompoundStore().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.CommandText.ShouldContain(clause, Case.Sensitive);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5099")]
    public async Task ACrossJoinIsWrittenWithNoCondition()
    {
        var result = await _sut.Translate(JoinedRead("Cross", withCondition: false), CompoundStore().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var sql = result.Value!.CommandText;
        sql.ShouldContain("CROSS JOIN [dbo].[Customer]", Case.Sensitive);
        sql.ShouldNotContain(" ON ");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5099")]
    [InlineData("Inner JOIN [x] ON 1=1; DROP TABLE y --", true, "CMDDATA-22030")]
    [InlineData("Outer", true, "CMDDATA-22030")]
    [InlineData("", true, "CMDDATA-22030")]
    [InlineData("None", true, "CMDDATA-22031")]
    [InlineData("Inner", false, "CMDDATA-22032")]
    [InlineData("Cross", true, "CMDDATA-22032")]
    public async Task AJoinTheStatementCannotStateIsRefusedWithItsOwnCodeAndNothingIsWritten(string joinType, bool withCondition, string code)
    {
        var result = await _sut.Translate(JoinedRead(joinType, withCondition), CompoundStore().Object, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(message => string.Equals(message.Code, code, StringComparison.Ordinal));
    }

    private static QueryCommand<object> CompoundRead(IOrderingExpression? ordering, IPagingExpression? paging) => new()
    {
        Joins = [new JoinExpression { TargetContainerName = "Customer", JoinType = "Inner", JoinConditions = [("customer_code", "code")] }],
        Projection = new ProjectionExpression
        {
            Fields =
            [
                new ProjectionField { PropertyName = "order_id", Alias = "OrderId", SourceContainer = "Order" },
                new ProjectionField { PropertyName = "name", Alias = "CustomerName", SourceContainer = "Customer" },
            ],
        },
        Filter = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "Customer.name", Operator = FilterOperators.ByName("Equal"), Value = "Ada" },
        },
        Ordering = ordering,
        Paging = paging,
    };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets.FDW4991")]
    public async Task AJoinedReadOrdersByQualifiedColumnsAndPages()
    {
        var ordering = new OrderingExpression
        {
            OrderedFields =
            [
                new OrderedField { PropertyName = "Customer.name", Direction = SortDirections.Descending },
                new OrderedField { PropertyName = "Order.order_id", Direction = SortDirections.Ascending },
            ],
        };

        var result = await _sut.Translate(CompoundRead(ordering, new PagingExpression { Skip = 6, Take = 3 }), CompoundStore().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var sql = result.Value!.CommandText;
        sql.ShouldContain("SELECT [Order].[order_id] AS [OrderId], [Customer].[name] AS [CustomerName]");
        sql.ShouldContain("INNER JOIN [dbo].[Customer] ON [Order].[customer_code] = [Customer].[code]");
        sql.ShouldContain("WHERE [Customer].[name] = @p0");
        sql.ShouldEndWith("ORDER BY [Customer].[name] DESC, [Order].[order_id] ASC OFFSET 6 ROWS FETCH NEXT 3 ROWS ONLY");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets.FDW4991")]
    public async Task APagedJoinThatStatesNoOrderIsRefused()
    {
        var result = await _sut.Translate(CompoundRead(null, new PagingExpression { Skip = 0, Take = 3 }), CompoundStore().Object, TestContext.Current.CancellationToken);

        // A join repeats its primary rows, so the primary key is no order to page by: none is guessed.
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets.FDW4991")]
    public async Task AnUnpagedJoinWithNoOrderSendsNeither()
    {
        var result = await _sut.Translate(CompoundRead(null, null), CompoundStore().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.CommandText.ShouldNotContain("ORDER BY");
        result.Value.CommandText.ShouldNotContain("OFFSET");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateSelectWithProjection()
    {
        var container = CreateContainer();
        var projection = new ProjectionExpression
        {
            Fields =
            [
                new ProjectionField { PropertyName = "Id" },
                new ProjectionField { PropertyName = "Name" }
            ]
        };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Joins).Returns([]);
        queryCommand.Setup(q => q.Filter).Returns((IFilterExpression?)null);
        queryCommand.Setup(q => q.Projection).Returns(projection);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldBe("SELECT [Id], [Name] FROM [dbo].[Customers]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateSelectWithSimpleWhereClause()
    {
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "Name",
                Operator = new EqualOperator(),
                Value = "Acme"
            }
        };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Joins).Returns([]);
        queryCommand.Setup(q => q.Filter).Returns(filter);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("WHERE [Name] = @p0");
        result.Value.Parameters.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateSelectWithAndGroupFilter()
    {
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.And,
                Nodes =
                [
                    new FilterCondition { PropertyName = "Name", Operator = new EqualOperator(), Value = "Acme" },
                    new FilterCondition { PropertyName = "Status", Operator = new EqualOperator(), Value = "Active" }
                ]
            }
        };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Joins).Returns([]);
        queryCommand.Setup(q => q.Filter).Returns(filter);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("WHERE ([Name] = @p0 AND [Status] = @p1)");
        result.Value.Parameters.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateSelectWithOrGroupFilter()
    {
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.Or,
                Nodes =
                [
                    new FilterCondition { PropertyName = "Status", Operator = new EqualOperator(), Value = "Active" },
                    new FilterCondition { PropertyName = "Status", Operator = new EqualOperator(), Value = "Pending" }
                ]
            }
        };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Joins).Returns([]);
        queryCommand.Setup(q => q.Filter).Returns(filter);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("WHERE ([Status] = @p0 OR [Status] = @p1)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateSelectWithIsNullFilterOmitsParameter()
    {
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "DeletedDate",
                Operator = new IsNullOperator(),
                Value = null
            }
        };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Joins).Returns([]);
        queryCommand.Setup(q => q.Filter).Returns(filter);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("WHERE [DeletedDate] IS NULL");
        result.Value.Parameters.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateSelectWithOrderByAscending()
    {
        var container = CreateContainer();

        var orderedField = new Mock<IOrderedField>();
        orderedField.Setup(f => f.PropertyName).Returns("Name");
        orderedField.Setup(f => f.Direction).Returns(new AscendingDirection());

        var ordering = new OrderingExpression
        {
            OrderedFields = [orderedField.Object]
        };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Joins).Returns([]);
        queryCommand.Setup(q => q.Filter).Returns((IFilterExpression?)null);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns(ordering);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("ORDER BY [Name] ASC");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateSelectWithOrderByDescending()
    {
        var container = CreateContainer();

        var orderedField = new Mock<IOrderedField>();
        orderedField.Setup(f => f.PropertyName).Returns("CreatedDate");
        orderedField.Setup(f => f.Direction).Returns(new DescendingDirection());

        var ordering = new OrderingExpression
        {
            OrderedFields = [orderedField.Object]
        };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Joins).Returns([]);
        queryCommand.Setup(q => q.Filter).Returns((IFilterExpression?)null);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns(ordering);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("ORDER BY [CreatedDate] DESC");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateSelectWithPaging()
    {
        var fields = new[] { CreateField("Id").Object };
        var container = CreateContainer(fields: fields, declaredKey: "Id");

        var paging = new PagingExpression { Skip = 20, Take = 10 };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Joins).Returns([]);
        queryCommand.Setup(q => q.Filter).Returns((IFilterExpression?)null);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns(paging);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("ORDER BY [Id]");
        result.Value.CommandText.ShouldContain("OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateSelectWithPagingRefusesAContainerThatDeclaresNoPrimaryKeyInsteadOfGuessingAnOrder()
    {
        // FDW-3454: the order was the first field, then the first returned field, then "1"; a guess is not an order.
        var fields = new[] { CreateField("Name").Object, CreateField("Email").Object };
        var container = CreateContainer(fields: fields);

        var paging = new PagingExpression { Skip = 0, Take = 50 };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Joins).Returns([]);
        queryCommand.Setup(q => q.Filter).Returns((IFilterExpression?)null);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns(paging);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldEndWith("-21020");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Customers");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateSelectWithPagingOrdersByTheDeclaredKeyNotByTheFirstField()
    {
        var fields = new[] { CreateField("Name").Object, CreateField("Email").Object };
        var container = CreateContainer(fields: fields, declaredKey: "Email");

        var paging = new PagingExpression { Skip = 0, Take = 50 };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Joins).Returns([]);
        queryCommand.Setup(q => q.Filter).Returns((IFilterExpression?)null);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns(paging);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("ORDER BY [Email]");
        result.Value.CommandText.ShouldNotContain("ORDER BY [Name]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateSelectWithPagingAndAStatedOrderingNeedsNoDeclaredKey()
    {
        var fields = new[] { CreateField("Name").Object, CreateField("Email").Object };
        var container = CreateContainer(fields: fields);
        var orderedField = new Mock<IOrderedField>();
        orderedField.Setup(f => f.PropertyName).Returns("Name");
        orderedField.Setup(f => f.Direction).Returns(new AscendingDirection());
        var ordering = new OrderingExpression { OrderedFields = [orderedField.Object] };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Joins).Returns([]);
        queryCommand.Setup(q => q.Filter).Returns((IFilterExpression?)null);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns(ordering);
        queryCommand.Setup(q => q.Paging).Returns(new PagingExpression { Skip = 0, Take = 50 });

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("ORDER BY [Name]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateReturnsFailureForNullContainer()
    {
        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Joins).Returns([]);
        var result = await _sut.Translate(queryCommand.Object, null!, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateReturnsFailureForNonDatabasePath()
    {
        var mockPath = new Mock<IPath>();
        var mockSchema = new Mock<IContainerSchema>();
        mockSchema.Setup(s => s.Fields).Returns([]);
        mockSchema.Setup(s => s.GetReturnedFields(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns([]);

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Path).Returns(mockPath.Object);
        container.Setup(c => c.Schema).Returns(mockSchema.Object);

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Joins).Returns([]);
        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateBaseOverloadReturnsFailureForNonQueryCommand()
    {
        var container = CreateContainer();
        var genericCommand = new Mock<IDataCommand>();

        var result = await _sut.Translate(genericCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateBaseOverloadDispatchesToQueryOverloadForIQueryCommand()
    {
        var container = CreateContainer(fields: [CreateField("Id").Object]);
        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Joins).Returns([]);
        queryCommand.Setup(q => q.Filter).Returns((IFilterExpression?)null);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        // Call the base overload (IDataCommand)
        IDataCommand dataCommand = queryCommand.Object;
        var result = await _sut.Translate(dataCommand, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("SELECT");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateSelectWithDatabaseQualifiedPath()
    {
        var dbPath = new DatabasePath("Northwind", "dbo", "Customers");
        var fields = new[] { CreateField("Id").Object, CreateField("Name").Object };
        var containerSchema = new Mock<IContainerSchema>();
        containerSchema.Setup(s => s.Fields).Returns(fields);
        containerSchema.Setup(s => s.GetReturnedFields(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns(fields);

        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns("Customers");
        container.As<IStorageContainer>().Setup(c => c.Path).Returns(dbPath);
        container.Setup(c => c.Schema).Returns(containerSchema.Object);
        container.Setup(c => c.ReferencingKeys)
            .Returns(GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success([]));
        container.Setup(c => c.Keys).Returns(new List<IContainerKey>());

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Joins).Returns([]);
        queryCommand.Setup(q => q.Filter).Returns((IFilterExpression?)null);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldBe("SELECT [Id], [Name] FROM [Northwind].[dbo].[Customers]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateSelectWithNestedFilterGroup()
    {
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.And,
                Nodes =
                [
                    new FilterGroup
                    {
                        Operator = LogicalOperator.Or,
                        Nodes =
                        [
                            new FilterCondition { PropertyName = "Name", Operator = new EqualOperator(), Value = "A" },
                            new FilterCondition { PropertyName = "Name", Operator = new EqualOperator(), Value = "B" }
                        ]
                    },
                    new FilterCondition { PropertyName = "Active", Operator = new EqualOperator(), Value = true }
                ]
            }
        };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Joins).Returns([]);
        queryCommand.Setup(q => q.Filter).Returns(filter);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("(([Name] = @p0 OR [Name] = @p1) AND [Active] = @p2)");
        result.Value.Parameters.Count.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsName()
    {
        _sut.Name.ShouldBe("Query");
    }


    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task BuildSelectClauseEmitsExplicitColumnsForRootContainer()
    {
        var fields = new[]
        {
            CreateField("Id").Object,
            CreateField("Name").Object,
        };
        var container = CreateContainer(fields: fields);
        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Joins).Returns([]);
        queryCommand.Setup(q => q.Filter).Returns((IFilterExpression?)null);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldNotContain("SELECT *");
        result.Value.CommandText.ShouldStartWith("SELECT [Id], [Name] FROM ");
    }

    // ── FDW-996: pattern operators are LIKE patterns, through the T-SQL dialect ──

    private async Task<(string Sql, object? Parameter)> TranslateOnly(FilterOperatorBase op, object? value)
    {
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "Name", Operator = op, Value = value },
        };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Joins).Returns([]);
        queryCommand.Setup(q => q.Filter).Returns(filter);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Parameters.Count.ShouldBe(1);
        return (result.Value.CommandText, result.Value.Parameters[0].Value);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task ContainsBecomesALikeWithWildcardsOnBothSidesAndAnEscapeClause()
    {
        var (sql, parameter) = await TranslateOnly(new ContainsOperator(), "afc");

        sql.ShouldContain("WHERE [Name] LIKE @p0 ESCAPE '!'");
        parameter.ShouldBe("%afc%");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task StartsWithBecomesALikeWithATrailingWildcard()
    {
        var (sql, parameter) = await TranslateOnly(new StartsWithOperator(), "afc");

        sql.ShouldContain("WHERE [Name] LIKE @p0 ESCAPE '!'");
        parameter.ShouldBe("afc%");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task EndsWithBecomesALikeWithALeadingWildcard()
    {
        var (sql, parameter) = await TranslateOnly(new EndsWithOperator(), "afc");

        sql.ShouldContain("WHERE [Name] LIKE @p0 ESCAPE '!'");
        parameter.ShouldBe("%afc");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task ABracketUnderscorePercentAndTheEscapeCharacterAreEscapedForTSql()
    {
        var (_, parameter) = await TranslateOnly(new ContainsOperator(), "a[b]_c%d!");

        parameter.ShouldBe("%a![b]!_c!%d!!%");
    }
}
