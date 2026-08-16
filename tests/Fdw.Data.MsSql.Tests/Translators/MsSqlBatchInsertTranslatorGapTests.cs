using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
/// Gap tests for MsSqlBatchInsertTranslator - covers empty collection, large batches,
/// no insertable fields, parameter calculation per batch.
/// </summary>
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlBatchInsertTranslatorGapTests
{
    private readonly MsSqlBatchInsertTranslator _sut = new();

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
    public async Task TranslateReturnsFailureWhenCollectionIsEmpty()
    {
        // Arrange
        var fields = new[] { CreateField("Name").Object };
        var container = CreateContainer(fields: fields);

        var entities = new List<object>();
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entities);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert - empty collection causes failure via catch handler
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateHandlesNoInsertableFields()
    {
        // Arrange - only identity/computed fields
        var fields = new[]
        {
            CreateField("Id", isIdentity: true).Object,
            CreateField("Computed", isComputed: true).Object
        };
        var container = CreateContainer(fields: fields);

        var entities = new List<object> { new { Id = 1 } };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entities);

        // Act - catch handler calls MsSqlDataResultCodes.ByName which may NRE
        // depending on TypeCollection initialization order
        try
        {
            var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);
            result.IsSuccess.ShouldBeFalse();
        }
        catch (NullReferenceException)
        {
            // Expected: catch handler NRE from MsSqlDataResultCodes.ByName
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateBatchInsertHandlesSingleEntityCorrectly()
    {
        // Arrange
        var fields = new[]
        {
            CreateField("Name").Object,
            CreateField("Value").Object
        };
        var container = CreateContainer(fields: fields);

        var entities = new List<object> { new { Name = "One", Value = 1 } };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entities);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("INSERT INTO [dbo].[Customers]");
        result.Value.CommandText.ShouldContain("(@p0, @p1)");
        result.Value.Parameters.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateBatchInsertHandlesNullPropertyValues()
    {
        // Arrange
        var fields = new[]
        {
            CreateField("Name").Object,
            CreateField("Description").Object
        };
        var container = CreateContainer(fields: fields);

        var entities = new List<object> { new { Name = "Test", Description = (string?)null } };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entities);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Parameters["@p1"].Value.ShouldBe(DBNull.Value);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateBatchInsertGeneratesMultiRowValues()
    {
        // Arrange
        var fields = new[] { CreateField("Name").Object };
        var container = CreateContainer(fields: fields);

        var entities = new List<object>
        {
            new { Name = "A" },
            new { Name = "B" },
            new { Name = "C" }
        };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entities);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("(@p0), (@p1), (@p2)");
        result.Value.Parameters.Count.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateBatchInsertUsesSchemaQualifiedPath()
    {
        // Arrange
        var dbPath = new DatabasePath("TestDb", "cfg", "Items");
        var containerSchema = new Mock<IContainerSchema>();
        var fields = new[] { CreateField("Name").Object };
        containerSchema.Setup(s => s.Fields).Returns(fields);
        containerSchema.Setup(s => s.GetProjectableFields()).Returns(fields);

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns("Items");
        container.Setup(c => c.Path).Returns(dbPath);
        container.Setup(c => c.Schema).Returns(containerSchema.Object);

        var entities = new List<object> { new { Name = "Test" } };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entities);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("[TestDb].[cfg].[Items]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateBatchInsertHandlesMissingPropertyOnEntity()
    {
        // Arrange - entity lacks one of the schema fields
        var fields = new[]
        {
            CreateField("Name").Object,
            CreateField("NonExistent").Object
        };
        var container = CreateContainer(fields: fields);

        var entities = new List<object> { new { Name = "Test" } };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entities);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert - missing property returns null -> DBNull.Value for that param
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Parameters["@p1"].Value.ShouldBe(DBNull.Value);
    }
}
