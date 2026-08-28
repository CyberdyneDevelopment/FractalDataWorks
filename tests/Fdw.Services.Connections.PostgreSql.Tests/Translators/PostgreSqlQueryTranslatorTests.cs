using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.PostgreSql.Tests.Translators;

[Collection(nameof(PostgreSqlTestCollection))]
public sealed class PostgreSqlQueryTranslatorTests
{
    private readonly PostgreSqlQueryTranslator _sut = new();

    private static Mock<IDataContainer> CreateContainer(
        string name = "customers",
        string schema = "public",
        string database = "",
        IField[]? fields = null)
    {
        var dbPath = new PostgreSqlDatabasePath(database, schema, name);
        var containerSchema = new Mock<IContainerSchema>();
        containerSchema.Setup(s => s.Fields).Returns(fields ?? new[] { CreateField("id").Object });
        containerSchema.Setup(s => s.GetProjectableFields()).Returns(fields ?? new[] { CreateField("id").Object });

        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns(name);
        container.As<IStorageContainer>().Setup(c => c.Path).Returns(dbPath);
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
        field.Setup(f => f.Name).Returns(name);
        field.Setup(f => f.IsIdentity).Returns(isIdentity);
        field.Setup(f => f.IsComputed).Returns(isComputed);
        return field;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void ConstructorSetsName()
    {
        _sut.Name.ShouldBe("Query");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateSelectFieldsUsesDoubleQuoteQuoting()
    {
        var fields = new[]
        {
            CreateField("id").Object,
            CreateField("name").Object,
            CreateField("email").Object
        };
        var container = CreateContainer(fields: fields);

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Filter).Returns((IFilterExpression?)null);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldBe("SELECT \"id\", \"name\", \"email\" FROM \"public\".\"customers\"");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateFromClauseUsesDoubleQuotedSchemaAndTable()
    {
        var container = CreateContainer(name: "orders", schema: "sales");

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Filter).Returns((IFilterExpression?)null);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("FROM \"sales\".\"orders\"");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateWhereClauseUsesDoubleQuoteQuotingAndAtParam()
    {
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "name",
                Operator = new EqualOperator(),
                Value = "Acme"
            }
        };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Filter).Returns(filter);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("WHERE \"name\" = @p0");
        result.Value.Parameters.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task TranslatePagingUsesLimitOffset()
    {
        var container = CreateContainer(fields: new[] { CreateField("id").Object });
        var paging = new PagingExpression { Skip = 20, Take = 10 };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Filter).Returns((IFilterExpression?)null);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns(paging);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("LIMIT 10 OFFSET 20");
        result.Value.CommandText.ShouldNotContain("FETCH NEXT");
        result.Value.CommandText.ShouldNotContain("ROWS ONLY");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task TranslatePagingInjectsDefaultOrderByFirstFieldWhenNoOrdering()
    {
        var fields = new[] { CreateField("name").Object, CreateField("email").Object };
        var container = CreateContainer(fields: fields);
        var paging = new PagingExpression { Skip = 0, Take = 50 };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Filter).Returns((IFilterExpression?)null);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns(paging);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        // Default ORDER BY uses dialect-quoted first field name
        result.Value!.CommandText.ShouldContain("ORDER BY \"name\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateOrderByAscendingUsesDoubleQuoteQuoting()
    {
        var container = CreateContainer();

        var orderedField = new Mock<IOrderedField>();
        orderedField.Setup(f => f.PropertyName).Returns("created_at");
        orderedField.Setup(f => f.Direction).Returns(new AscendingDirection());

        var ordering = new OrderingExpression
        {
            OrderedFields = [orderedField.Object]
        };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Filter).Returns((IFilterExpression?)null);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns(ordering);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("ORDER BY \"created_at\" ASC");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateOrderByDescendingUsesDoubleQuoteQuoting()
    {
        var container = CreateContainer();

        var orderedField = new Mock<IOrderedField>();
        orderedField.Setup(f => f.PropertyName).Returns("created_at");
        orderedField.Setup(f => f.Direction).Returns(new DescendingDirection());

        var ordering = new OrderingExpression
        {
            OrderedFields = [orderedField.Object]
        };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Filter).Returns((IFilterExpression?)null);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns(ordering);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("ORDER BY \"created_at\" DESC");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateAndGroupFilterUsesDoubleQuoteQuoting()
    {
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.And,
                Nodes =
                [
                    new FilterCondition { PropertyName = "name", Operator = new EqualOperator(), Value = "Acme" },
                    new FilterCondition { PropertyName = "status", Operator = new EqualOperator(), Value = "active" }
                ]
            }
        };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Filter).Returns(filter);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("WHERE (\"name\" = @p0 AND \"status\" = @p1)");
        result.Value.Parameters.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateOrGroupFilterUsesDoubleQuoteQuoting()
    {
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.Or,
                Nodes =
                [
                    new FilterCondition { PropertyName = "status", Operator = new EqualOperator(), Value = "active" },
                    new FilterCondition { PropertyName = "status", Operator = new EqualOperator(), Value = "pending" }
                ]
            }
        };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Filter).Returns(filter);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("WHERE (\"status\" = @p0 OR \"status\" = @p1)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateIsNullFilterOmitsParameter()
    {
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "deleted_at",
                Operator = new IsNullOperator(),
                Value = null
            }
        };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Filter).Returns(filter);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("WHERE \"deleted_at\" IS NULL");
        result.Value.Parameters.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateEmptyInListEmitsFALSENotOneEqualsZero()
    {
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "id",
                Operator = new InOperator(),
                Value = Array.Empty<int>()   // empty IN list
            }
        };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Filter).Returns(filter);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        // PG always-false predicate is FALSE, not "1 = 0"
        result.Value!.CommandText.ShouldContain("WHERE FALSE");
        result.Value.CommandText.ShouldNotContain("1 = 0");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateNonEmptyInListExpandsParams()
    {
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "id",
                Operator = new InOperator(),
                Value = new[] { 1, 2, 3 }
            }
        };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Filter).Returns(filter);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("\"id\" IN (@p0_0, @p0_1, @p0_2)");
        result.Value.Parameters.Count.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateReturnsFailureForNullContainer()
    {
        var queryCommand = new Mock<IQueryCommand>();
        var result = await _sut.Translate(queryCommand.Object, null!, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateReturnsFailureForNonDatabasePath()
    {
        var mockPath = new Mock<IPath>();
        var mockSchema = new Mock<IContainerSchema>();
        mockSchema.Setup(s => s.Fields).Returns([]);
        mockSchema.Setup(s => s.GetProjectableFields()).Returns([]);

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Path).Returns(mockPath.Object);
        container.Setup(c => c.Schema).Returns(mockSchema.Object);

        var queryCommand = new Mock<IQueryCommand>();
        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateReturnsFailureForNonQueryCommand()
    {
        var container = CreateContainer();
        var genericCommand = new Mock<IDataCommand>();

        var result = await _sut.Translate(genericCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateWithNoFieldsAndNoProjectionFailsLoud()
    {
        var container = CreateContainer(fields: []);
        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Filter).Returns((IFilterExpression?)null);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateSelectDoesNotEmitSelectStar()
    {
        var fields = new[] { CreateField("id").Object, CreateField("name").Object };
        var container = CreateContainer(fields: fields);

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Filter).Returns((IFilterExpression?)null);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldNotContain("SELECT *");
    }
}
