using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.MsSql.Tests.Translators;

[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlInsertTranslatorTests
{
    private readonly MsSqlInsertTranslator _sut = new();

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
    public async Task TranslateInsertGeneratesCorrectSql()
    {
        var fields = new[]
        {
            CreateField("Id", isIdentity: true).Object,
            CreateField("Name").Object,
            CreateField("Email").Object
        };
        var container = CreateContainer(fields: fields);

        var entity = new { Name = "Acme", Email = "info@acme.com" };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(new[] { entity });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("INSERT INTO [dbo].[Customers]");
        result.Value.CommandText.ShouldContain("([Name], [Email])");
        result.Value.CommandText.ShouldContain("VALUES (@p0, @p1)");
        result.Value.CommandText.ShouldContain("SELECT @@ROWCOUNT");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateInsertExcludesIdentityColumns()
    {
        var fields = new[]
        {
            CreateField("Id", isIdentity: true).Object,
            CreateField("Name").Object
        };
        var container = CreateContainer(fields: fields);

        var entity = new { Name = "Test" };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(new[] { entity });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldNotContain("[Id]");
        result.Value.CommandText.ShouldContain("[Name]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateInsertExcludesComputedColumns()
    {
        var fields = new[]
        {
            CreateField("Name").Object,
            CreateField("FullName", isComputed: true).Object
        };
        var container = CreateContainer(fields: fields);

        var entity = new { Name = "Test" };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(new[] { entity });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldNotContain("[FullName]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateInsertReturnsFailureForNullContainer()
    {
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(new[] { new { Name = "Test" } });

        var result = await _sut.Translate(command.Object, null!, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateInsertReturnsFailureForNonDatabasePath()
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
        command.Setup(c => c.InputData).Returns(new[] { new { Name = "Test" } });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateInsertReturnsFailureWhenNoInputData()
    {
        var fields = new[] { CreateField("Name").Object };
        var container = CreateContainer(fields: fields);

        // IDataCommand without IDataCommandWithInput
        var command = new Mock<IDataCommand>();

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateInsertLeavesOutAColumnTheRowLacksSoTheDatabaseDefaultApplies()
    {
        var fields = new[]
        {
            CreateField("Name").Object,
            CreateField("CreatedAt").Object
        };
        var container = CreateContainer(fields: fields);

        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(new[] { new { Name = "Acme" } });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("([Name])");
        result.Value.CommandText.ShouldNotContain("[CreatedAt]");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateInsertOfManyRowsWritesOneStatementWithARowPerValuesClause()
    {
        var fields = new[]
        {
            CreateField("Name").Object,
            CreateField("Email").Object
        };
        var container = CreateContainer(fields: fields);

        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(new[]
        {
            new { Name = "One", Email = "one@x.com" },
            new { Name = "Two", Email = "two@x.com" },
            new { Name = "Three", Email = "three@x.com" }
        });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("VALUES (@p0, @p1), (@p2, @p3), (@p4, @p5)");
        result.Value.Parameters.Count.ShouldBe(6);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateInsertBindsNullForAColumnOnlySomeRowsSupply()
    {
        var fields = new[]
        {
            CreateField("Name").Object,
            CreateField("Email").Object
        };
        var container = CreateContainer(fields: fields);

        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(new List<object>
        {
            new Dictionary<string, object?> { ["Name"] = "One", ["Email"] = "one@x.com" },
            new Dictionary<string, object?> { ["Name"] = "Two" }
        });

        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("([Name], [Email])");
        result.Value.Parameters.Count.ShouldBe(4);
        result.Value.Parameters[3].Value.ShouldBe(DBNull.Value);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateInsertRefusesInputThatIsNotRows()
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
    public void ConstructorSetsName()
    {
        _sut.Name.ShouldBe("Insert");
    }
}
