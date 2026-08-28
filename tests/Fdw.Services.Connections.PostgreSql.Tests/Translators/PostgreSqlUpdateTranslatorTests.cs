using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.PostgreSql.Tests.Translators;

[Collection(nameof(PostgreSqlTestCollection))]
public sealed class PostgreSqlUpdateTranslatorTests
{
    private readonly PostgreSqlUpdateTranslator _sut = new();

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

    private static Mock<IStorageContainer> CreateContainer(
        string name = "customers",
        IField[]? fields = null,
        string? primaryKeyFieldName = null)
    {
        var dbPath = new PostgreSqlDatabasePath(null, "public", name);
        var containerSchema = new Mock<IContainerSchema>();
        containerSchema.Setup(s => s.Fields).Returns(fields ?? []);
        containerSchema.Setup(s => s.GetProjectableFields()).Returns(fields ?? []);

        var metadata = new Dictionary<string, object>();
        if (primaryKeyFieldName != null)
            metadata["SurrogateKeyField"] = primaryKeyFieldName;

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns(name);
        container.Setup(c => c.Path).Returns(dbPath);
        container.Setup(c => c.Schema).Returns(containerSchema.Object);
        container.Setup(c => c.Metadata).Returns(metadata);

        return container;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void ConstructorSetsName()
    {
        _sut.Name.ShouldBe("Update");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateUpdateUsesDoubleQuoteQuoting()
    {
        var fields = new[]
        {
            CreateField("id").Object,
            CreateField("name").Object,
            CreateField("email").Object
        };
        var container = CreateContainer(fields: fields, primaryKeyFieldName: "id");

        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "id",
                Operator = new EqualOperator(),
                Value = 42
            }
        };

        var entity = new { name = "Updated", email = "new@test.com" };

        var command = new Mock<IFilterableCommand>();
        command.Setup(c => c.Filter).Returns(filter);
        command.As<IDataCommandWithInput>().Setup(c => c.InputData).Returns(entity);

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("UPDATE \"public\".\"customers\" SET");
        result.Value.CommandText.ShouldContain("\"name\" = @set_name");
        result.Value.CommandText.ShouldContain("\"email\" = @set_email");
        // Primary key must not appear in SET clause
        result.Value.CommandText.ShouldNotContain("\"id\" = @set_id");
        result.Value.CommandText.ShouldContain("WHERE \"id\" = @where_p0");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateUpdateExcludesPrimaryKeyFromSetClause()
    {
        var fields = new[]
        {
            CreateField("id").Object,
            CreateField("name").Object
        };
        var container = CreateContainer(fields: fields, primaryKeyFieldName: "id");

        var filter = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "id", Operator = new EqualOperator(), Value = 1 }
        };

        var entity = new { name = "Test" };
        var command = new Mock<IFilterableCommand>();
        command.Setup(c => c.Filter).Returns(filter);
        command.As<IDataCommandWithInput>().Setup(c => c.InputData).Returns(entity);

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldNotContain("SET \"id\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateUpdateReturnsFailureForNullContainer()
    {
        var command = new Mock<IFilterableCommand>();
        command.As<IDataCommandWithInput>().Setup(c => c.InputData).Returns(new { name = "Test" });

        var result = await _sut.Translate(command.Object, null!, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateUpdateReturnsFailureForNonDatabasePath()
    {
        var mockPath = new Mock<IPath>();
        var mockSchema = new Mock<IContainerSchema>();
        mockSchema.Setup(s => s.Fields).Returns([]);
        mockSchema.Setup(s => s.GetProjectableFields()).Returns([]);

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Path).Returns(mockPath.Object);
        container.Setup(c => c.Schema).Returns(mockSchema.Object);

        var command = new Mock<IFilterableCommand>();
        command.As<IDataCommandWithInput>().Setup(c => c.InputData).Returns(new { name = "Test" });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateUpdateReturnsFailureWhenNoInputData()
    {
        var fields = new[] { CreateField("name").Object };
        var container = CreateContainer(fields: fields);

        var command = new Mock<IFilterableCommand>();
        command.Setup(c => c.Filter).Returns((IFilterExpression?)null);

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
