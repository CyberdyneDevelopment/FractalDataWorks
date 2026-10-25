using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.MsSql.Tests.Translators;

[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlInsertTranslatorPlanTests
{
    private readonly MsSqlInsertTranslator _sut = new();

    private static Mock<IFieldType> CreateFieldType(string typeName = "String")
    {
        var ft = new Mock<IFieldType>();
        ft.Setup(f => f.ClrType).Returns(typeof(string));
        ft.Setup(f => f.TypeName).Returns(typeName);
        return ft;
    }

    private static Mock<IField> CreateField(
        string name,
        bool isIdentity = false,
        bool isComputed = false,
        bool isNullable = false,
        string typeName = "String")
    {
        var field = new Mock<IField>();
        field.Setup(f => f.Name).Returns(name);
        field.Setup(f => f.IsIdentity).Returns(isIdentity);
        field.Setup(f => f.IsComputed).Returns(isComputed);
        field.Setup(f => f.IsNullable).Returns(isNullable);
        field.Setup(f => f.FieldType).Returns(CreateFieldType(typeName).Object);
        return field;
    }

    private static Mock<IStorageContainer> CreateContainer(
        string name = "Customers",
        IField[]? fields = null)
    {
        var dbPath = new DatabasePath("", "dbo", name);
        var containerSchema = new Mock<IContainerSchema>();
        containerSchema.Setup(s => s.Fields).Returns(fields ?? []);
        containerSchema.Setup(s => s.GetReturnedFields(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns(fields ?? []);

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns(name);
        container.Setup(c => c.Path).Returns(dbPath);
        container.Setup(c => c.Schema).Returns(containerSchema.Object);

        return container;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateReturnsFailureForNullContainer()
    {
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(new List<object>());

        var result = await _sut.Translate(command.Object, null!, TestContext.Current.CancellationToken);

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

        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(new List<object>());

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateReturnsFailureWhenNoInputData()
    {
        var fields = new[] { CreateField("Name").Object };
        var container = CreateContainer(fields: fields);

        var command = new Mock<IDataCommand>();

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateReturnsFailureWhenDataIsNotEnumerable()
    {
        var fields = new[] { CreateField("Name").Object };
        var container = CreateContainer(fields: fields);

        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(42);

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task PlanGeneratesBatchedInsertSql()
    {
        var fields = new[]
        {
            CreateField("Id", isIdentity: true).Object,
            CreateField("Name").Object,
            CreateField("Email").Object
        };
        var container = CreateContainer(fields: fields);

        var entities = new List<object>
        {
            new { Name = "Acme", Email = "a@acme.com" },
            new { Name = "Beta", Email = "b@beta.com" }
        };

        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entities);

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // A real SQL command (FDW-1152): no marker, no stored procedure, the rows bound as parameters.
        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandType.ShouldBe(System.Data.CommandType.Text);
        result.Value.CommandText.ShouldContain("INSERT INTO [dbo].[Customers]");
        result.Value.CommandText.ShouldNotContain("MARKER");
        result.Value.Parameters.Count.ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task PlanExcludesIdentityAndComputedColumns()
    {
        var fields = new[]
        {
            CreateField("Id", isIdentity: true).Object,
            CreateField("Name").Object,
            CreateField("Computed", isComputed: true).Object
        };
        var container = CreateContainer(fields: fields);

        var entities = new List<object> { new { Name = "Test" } };

        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entities);

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("[Name]");
        result.Value.CommandText.ShouldNotContain("[Id]");
        result.Value.CommandText.ShouldNotContain("[Computed]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void PlanMapsTheInsertableColumnsToTheSchemaQualifiedTable()
    {
        var fields = new[]
        {
            CreateField("Id", isIdentity: true).Object,
            CreateField("Name").Object,
            CreateField("Computed", isComputed: true).Object
        };
        var container = CreateContainer(fields: fields);

        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(new List<object> { new { Name = "Test" }, new { Name = "Other" } });

        var result = MsSqlInsertTranslator.Plan(command.Object, container.Object);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Destination.ShouldBe("[dbo].[Customers]");
        result.Value.Columns.ShouldBe(["Name"]);
        result.Value.Rows.Rows.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void PlanReturnsFailureWhenTheCommandCarriesNoRows()
    {
        var container = CreateContainer(fields: [CreateField("Name").Object]);

        var result = MsSqlInsertTranslator.Plan(new Mock<IDataCommand>().Object, container.Object);

        result.IsSuccess.ShouldBeFalse();
    }
}
