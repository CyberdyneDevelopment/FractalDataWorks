using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.PostgreSql.Tests.Translators;

[Collection(nameof(PostgreSqlTestCollection))]
public sealed class PostgreSqlInsertTranslatorTests
{
    private readonly PostgreSqlInsertTranslator _sut = new();

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
        IField[]? fields = null)
    {
        var dbPath = new PostgreSqlDatabasePath(null, "public", name);
        var containerSchema = new Mock<IContainerSchema>();
        containerSchema.Setup(s => s.Fields).Returns(fields ?? []);
        containerSchema.Setup(s => s.GetProjectableFields()).Returns(fields ?? []);

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
        _sut.Name.ShouldBe("Insert");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateInsertUsesDoubleQuoteQuotingAndReturningClause()
    {
        var fields = new[]
        {
            CreateField("id", isIdentity: true).Object,
            CreateField("name").Object,
            CreateField("email").Object
        };
        var container = CreateContainer(fields: fields);

        var entity = new { name = "Acme", email = "info@acme.com" };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entity);

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("INSERT INTO \"public\".\"customers\"");
        result.Value.CommandText.ShouldContain("(\"name\", \"email\")");
        result.Value.CommandText.ShouldContain("RETURNING \"id\"");
        // Must NOT contain T-SQL SCOPE_IDENTITY()
        result.Value.CommandText.ShouldNotContain("SCOPE_IDENTITY");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateInsertExcludesIdentityColumns()
    {
        var fields = new[]
        {
            CreateField("id", isIdentity: true).Object,
            CreateField("name").Object
        };
        var container = CreateContainer(fields: fields);

        var entity = new { name = "Test" };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entity);

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldNotContain("\"id\",");
        result.Value.CommandText.ShouldContain("\"name\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateInsertExcludesComputedColumns()
    {
        var fields = new[]
        {
            CreateField("name").Object,
            CreateField("full_name", isComputed: true).Object
        };
        var container = CreateContainer(fields: fields);

        var entity = new { name = "Test" };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entity);

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldNotContain("\"full_name\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateInsertReturnsFailureForNullContainer()
    {
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(new { name = "Test" });

        var result = await _sut.Translate(command.Object, null!, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateInsertReturnsFailureForNonDatabasePath()
    {
        var mockPath = new Mock<IPath>();
        var mockSchema = new Mock<IContainerSchema>();
        mockSchema.Setup(s => s.Fields).Returns([]);
        mockSchema.Setup(s => s.GetProjectableFields()).Returns([]);

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Path).Returns(mockPath.Object);
        container.Setup(c => c.Schema).Returns(mockSchema.Object);

        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(new { name = "Test" });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateInsertReturnsFailureWhenNoInputData()
    {
        var fields = new[] { CreateField("name").Object };
        var container = CreateContainer(fields: fields);

        var command = new Mock<IDataCommand>();

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }
}
