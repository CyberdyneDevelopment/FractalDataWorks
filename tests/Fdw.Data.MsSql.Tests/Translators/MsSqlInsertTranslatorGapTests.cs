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

/// <summary>
/// Gap tests for MsSqlInsertTranslator - covers branches not exercised by existing tests.
/// </summary>
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlInsertTranslatorGapTests
{
    private readonly MsSqlInsertTranslator _sut = new();

    private static Mock<IField> CreateField(
        string name,
        bool isIdentity = false,
        bool isComputed = false)
    {
        var field = new Mock<IField>();
        field.Setup(f => f.Name).Returns(name);
        // Why: IsPrimaryKey removed from IField — PK identity resolved from container Metadata["SurrogateKeyField"].
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
        containerSchema.Setup(s => s.GetProjectableFields()).Returns(fields ?? []);

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns(name);
        container.Setup(c => c.Path).Returns(dbPath);
        container.Setup(c => c.Schema).Returns(containerSchema.Object);

        return container;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateReturnsFailureWhenAllFieldsAreIdentityOrComputed()
    {
        // Arrange - container with only identity/computed fields (no insertable fields)
        var fields = new[]
        {
            CreateField("Id", isIdentity: true).Object,
            CreateField("FullName", isComputed: true).Object
        };
        var container = CreateContainer(fields: fields);

        var entity = new { Id = 1, FullName = "Test" };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entity);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert - no insertable fields causes failure via catch handler
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateInsertGeneratesCorrectParameterValues()
    {
        // Arrange
        var fields = new[]
        {
            CreateField("Id", isIdentity: true).Object,
            CreateField("Name").Object,
            CreateField("Age").Object
        };
        var container = CreateContainer(fields: fields);

        var entity = new { Name = "TestUser", Age = 30 };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entity);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Parameters.Count.ShouldBe(2);
        result.Value.Parameters["@Name"].Value.ShouldBe("TestUser");
        result.Value.Parameters["@Age"].Value.ShouldBe(30);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateInsertHandlesNullPropertyValueAsDbNull()
    {
        // Arrange
        var fields = new[]
        {
            CreateField("Name").Object,
            CreateField("Description").Object
        };
        var container = CreateContainer(fields: fields);

        var entity = new { Name = "Test", Description = (string?)null };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entity);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Parameters["@Description"].Value.ShouldBe(DBNull.Value);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateInsertUsesSchemaQualifiedPath()
    {
        // Arrange - use a non-dbo schema
        var dbPath = new DatabasePath("TestDb", "sales", "Orders");
        var containerSchema = new Mock<IContainerSchema>();
        var fields = new[] { CreateField("ProductName").Object };
        containerSchema.Setup(s => s.Fields).Returns(fields);
        containerSchema.Setup(s => s.GetProjectableFields()).Returns(fields);

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns("Orders");
        container.Setup(c => c.Path).Returns(dbPath);
        container.Setup(c => c.Schema).Returns(containerSchema.Object);

        var entity = new { ProductName = "Widget" };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entity);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("[TestDb].[sales].[Orders]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateInsertWithMultipleFieldsIncludesAllInColumnAndParamLists()
    {
        // Arrange
        var fields = new[]
        {
            CreateField("FirstName").Object,
            CreateField("LastName").Object,
            CreateField("Email").Object,
            CreateField("Phone").Object
        };
        var container = CreateContainer(fields: fields);

        var entity = new { FirstName = "John", LastName = "Doe", Email = "john@test.com", Phone = "555-1234" };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entity);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("[FirstName], [LastName], [Email], [Phone]");
        result.Value.CommandText.ShouldContain("@FirstName, @LastName, @Email, @Phone");
        result.Value.Parameters.Count.ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateInsertHandsMissingPropertyGracefully()
    {
        // Arrange - entity doesn't have a property matching one of the schema fields
        var fields = new[]
        {
            CreateField("Name").Object,
            CreateField("MissingProp").Object
        };
        var container = CreateContainer(fields: fields);

        var entity = new { Name = "Test" };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entity);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert - should succeed, MissingProp is just not added as parameter
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Parameters.Count.ShouldBe(1);
    }
}
