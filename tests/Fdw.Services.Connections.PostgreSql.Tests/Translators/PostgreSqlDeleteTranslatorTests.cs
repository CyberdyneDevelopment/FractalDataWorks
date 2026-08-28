using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.PostgreSql.Tests.Translators;

[Collection(nameof(PostgreSqlTestCollection))]
public sealed class PostgreSqlDeleteTranslatorTests
{
    private readonly PostgreSqlDeleteTranslator _sut = new();

    private static Mock<IStorageContainer> CreateContainer(
        string name = "customers",
        string schema = "public")
    {
        var dbPath = new PostgreSqlDatabasePath(null, schema, name);
        var containerSchema = new Mock<IContainerSchema>();
        containerSchema.Setup(s => s.Fields).Returns([]);
        containerSchema.Setup(s => s.GetProjectableFields()).Returns([]);

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns(name);
        container.Setup(c => c.Path).Returns(dbPath);
        container.Setup(c => c.Schema).Returns(containerSchema.Object);

        return container;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void ConstructorSetsName()
    {
        _sut.Name.ShouldBe("Delete");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateDeleteUsesDoubleQuoteQuoting()
    {
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "id",
                Operator = new EqualOperator(),
                Value = 42
            }
        };

        var command = new Mock<IFilterableCommand>();
        command.Setup(c => c.Filter).Returns(filter);

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldBe("DELETE FROM \"public\".\"customers\" WHERE \"id\" = @p0");
        result.Value.Parameters.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateDeleteWithAndGroupFilter()
    {
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.And,
                Nodes =
                [
                    new FilterCondition { PropertyName = "status", Operator = new EqualOperator(), Value = "inactive" },
                    new FilterCondition { PropertyName = "is_deleted", Operator = new EqualOperator(), Value = true }
                ]
            }
        };

        var command = new Mock<IFilterableCommand>();
        command.Setup(c => c.Filter).Returns(filter);

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("WHERE (\"status\" = @p0 AND \"is_deleted\" = @p1)");
        result.Value.Parameters.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateDeleteReturnsFailureWhenFilterIsNull()
    {
        var container = CreateContainer();

        var command = new Mock<IFilterableCommand>();
        command.Setup(c => c.Filter).Returns((IFilterExpression?)null);

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateDeleteReturnsFailureWhenFilterRootIsNull()
    {
        var container = CreateContainer();
        var filter = new FilterExpression { Root = null };

        var command = new Mock<IFilterableCommand>();
        command.Setup(c => c.Filter).Returns(filter);

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateDeleteReturnsFailureForNullContainer()
    {
        var command = new Mock<IFilterableCommand>();
        command.Setup(c => c.Filter).Returns(new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "id", Operator = new EqualOperator(), Value = 1 }
        });

        var result = await _sut.Translate(command.Object, null!, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateDeleteReturnsFailureForNonDatabasePath()
    {
        var mockPath = new Mock<IPath>();
        var mockSchema = new Mock<IContainerSchema>();
        mockSchema.Setup(s => s.Fields).Returns([]);
        mockSchema.Setup(s => s.GetProjectableFields()).Returns([]);

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Path).Returns(mockPath.Object);
        container.Setup(c => c.Schema).Returns(mockSchema.Object);

        var command = new Mock<IFilterableCommand>();
        command.Setup(c => c.Filter).Returns(new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "id", Operator = new EqualOperator(), Value = 1 }
        });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateBaseOverloadReturnsFailureForNonFilterableCommand()
    {
        var container = CreateContainer();
        var genericCommand = new Mock<IDataCommand>();

        var result = await _sut.Translate(genericCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }
}
