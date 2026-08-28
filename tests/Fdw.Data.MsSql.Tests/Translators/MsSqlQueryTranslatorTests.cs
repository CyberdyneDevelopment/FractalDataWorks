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

namespace Fdw.Data.MsSql.Tests.Translators;

[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlQueryTranslatorTests
{
    private readonly MsSqlQueryTranslator _sut = new();

    private static Mock<IDataContainer> CreateContainer(
        string name = "Customers",
        string schema = "dbo",
        string database = "",
        IField[]? fields = null)
    {
        var dbPath = new DatabasePath(database, schema, name);
        var containerSchema = new Mock<IContainerSchema>();
        containerSchema.Setup(s => s.Fields).Returns(fields ?? new[] { CreateField("Id").Object });
        containerSchema.Setup(s => s.GetProjectableFields()).Returns(fields ?? new[] { CreateField("Id").Object });

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
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslatorFailsLoudWhenContainerHasNoFields()
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

        var call = new QueryCommandBuilder<object>("ConfigurationDb", "conn", "MsSqlConnection")
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
        var container = CreateContainer(fields: fields);

        var paging = new PagingExpression { Skip = 20, Take = 10 };

        var queryCommand = new Mock<IQueryCommand>();
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
    public async Task TranslateSelectWithPagingUsesFirstFieldWhenNoPrimaryKey()
    {
        var fields = new[] { CreateField("Name").Object, CreateField("Email").Object };
        var container = CreateContainer(fields: fields);

        var paging = new PagingExpression { Skip = 0, Take = 50 };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Filter).Returns((IFilterExpression?)null);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns(paging);

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
        containerSchema.Setup(s => s.GetProjectableFields()).Returns(fields);

        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns("Customers");
        container.As<IStorageContainer>().Setup(c => c.Path).Returns(dbPath);
        container.Setup(c => c.Schema).Returns(containerSchema.Object);
        container.Setup(c => c.ReferencingKeys)
            .Returns(GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success([]));
        container.Setup(c => c.Keys).Returns(new List<IContainerKey>());

        var queryCommand = new Mock<IQueryCommand>();
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
        queryCommand.Setup(q => q.Filter).Returns((IFilterExpression?)null);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldNotContain("SELECT *");
        result.Value.CommandText.ShouldStartWith("SELECT [Id], [Name] FROM ");
    }

}
