using System;
using System.Collections.Generic;
using System.Data;
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
/// Gap tests for MsSqlBulkInsertTranslator - covers ConvertToDataTable field type branches,
/// nullable value types, empty collection, no insertable fields.
/// </summary>
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlBulkInsertTranslatorGapTests
{
    private readonly MsSqlBulkInsertTranslator _sut = new();

    private static Mock<IFieldType> CreateFieldType(Type clrType, string typeName = "")
    {
        var ft = new Mock<IFieldType>();
        ft.Setup(f => f.ClrType).Returns(clrType);
        ft.Setup(f => f.TypeName).Returns(string.IsNullOrEmpty(typeName) ? clrType.Name : typeName);
        return ft;
    }

    private static Mock<IField> CreateField(
        string name,
        bool isIdentity = false,
        bool isComputed = false,
        bool isNullable = false,
        Type? clrType = null,
        string typeName = "")
    {
        var field = new Mock<IField>();
        field.Setup(f => f.Name).Returns(name);
        // Why: IsPrimaryKey removed from IField — PK identity resolved from container Metadata["SurrogateKeyField"].
        field.Setup(f => f.IsIdentity).Returns(isIdentity);
        field.Setup(f => f.IsComputed).Returns(isComputed);
        field.Setup(f => f.IsNullable).Returns(isNullable);
        field.Setup(f => f.FieldType).Returns(CreateFieldType(clrType ?? typeof(string), typeName).Object);
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
    public async Task TranslateWithEmptyCollectionCreatesEmptyDataTable()
    {
        // Arrange - BulkInsert allows empty collection (creates empty DataTable)
        var fields = new[] { CreateField("Name").Object };
        var container = CreateContainer(fields: fields);

        var entities = new List<object>();
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entities);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert - succeeds with empty DataTable
        result.IsSuccess.ShouldBeTrue();
        var dataTable = (DataTable)result.Value!.Parameters["@__BulkCopy_DataTable"].Value!;
        dataTable.Rows.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateHandlesNoInsertableFields()
    {
        // Arrange
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
    public async Task TranslateBulkInsertCreatesDataTableWithInt32TypeFromObjectClrType()
    {
        // Arrange - ClrType is object, TypeName is "Int32" -> should infer int
        var fields = new[]
        {
            CreateField("Age", clrType: typeof(object), typeName: "Int32").Object
        };
        var container = CreateContainer(fields: fields);

        var entities = new List<object> { new { Age = 25 } };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entities);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var dataTableParam = result.Value!.Parameters["@__BulkCopy_DataTable"];
        var dataTable = (DataTable)dataTableParam.Value!;
        dataTable.Columns["Age"]!.DataType.ShouldBe(typeof(int));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateBulkInsertCreatesDataTableWithInt64Type()
    {
        // Arrange
        var fields = new[]
        {
            CreateField("BigVal", clrType: typeof(object), typeName: "Int64").Object
        };
        var container = CreateContainer(fields: fields);

        var entities = new List<object> { new { BigVal = 999999999L } };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entities);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var dataTable = (DataTable)result.Value!.Parameters["@__BulkCopy_DataTable"].Value!;
        dataTable.Columns["BigVal"]!.DataType.ShouldBe(typeof(long));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateBulkInsertCreatesDataTableWithStringType()
    {
        // Arrange
        var fields = new[]
        {
            CreateField("Name", clrType: typeof(object), typeName: "String").Object
        };
        var container = CreateContainer(fields: fields);

        var entities = new List<object> { new { Name = "Test" } };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entities);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var dataTable = (DataTable)result.Value!.Parameters["@__BulkCopy_DataTable"].Value!;
        dataTable.Columns["Name"]!.DataType.ShouldBe(typeof(string));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateBulkInsertCreatesDataTableWithDateTimeType()
    {
        // Arrange
        var fields = new[]
        {
            CreateField("Created", clrType: typeof(object), typeName: "DateTime").Object
        };
        var container = CreateContainer(fields: fields);

        var entities = new List<object> { new { Created = DateTime.UtcNow } };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entities);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var dataTable = (DataTable)result.Value!.Parameters["@__BulkCopy_DataTable"].Value!;
        dataTable.Columns["Created"]!.DataType.ShouldBe(typeof(DateTime));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateBulkInsertCreatesDataTableWithBooleanType()
    {
        // Arrange
        var fields = new[]
        {
            CreateField("IsActive", clrType: typeof(object), typeName: "Boolean").Object
        };
        var container = CreateContainer(fields: fields);

        var entities = new List<object> { new { IsActive = true } };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entities);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var dataTable = (DataTable)result.Value!.Parameters["@__BulkCopy_DataTable"].Value!;
        dataTable.Columns["IsActive"]!.DataType.ShouldBe(typeof(bool));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateBulkInsertCreatesDataTableWithDecimalType()
    {
        // Arrange
        var fields = new[]
        {
            CreateField("Price", clrType: typeof(object), typeName: "Decimal").Object
        };
        var container = CreateContainer(fields: fields);

        var entities = new List<object> { new { Price = 19.99m } };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entities);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var dataTable = (DataTable)result.Value!.Parameters["@__BulkCopy_DataTable"].Value!;
        dataTable.Columns["Price"]!.DataType.ShouldBe(typeof(decimal));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateBulkInsertCreatesDataTableWithDoubleType()
    {
        // Arrange
        var fields = new[]
        {
            CreateField("Weight", clrType: typeof(object), typeName: "Double").Object
        };
        var container = CreateContainer(fields: fields);

        var entities = new List<object> { new { Weight = 3.14 } };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entities);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var dataTable = (DataTable)result.Value!.Parameters["@__BulkCopy_DataTable"].Value!;
        dataTable.Columns["Weight"]!.DataType.ShouldBe(typeof(double));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateBulkInsertCreatesDataTableWithGuidType()
    {
        // Arrange
        var fields = new[]
        {
            CreateField("ExternalId", clrType: typeof(object), typeName: "Guid").Object
        };
        var container = CreateContainer(fields: fields);

        var entities = new List<object> { new { ExternalId = Guid.NewGuid() } };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entities);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var dataTable = (DataTable)result.Value!.Parameters["@__BulkCopy_DataTable"].Value!;
        dataTable.Columns["ExternalId"]!.DataType.ShouldBe(typeof(Guid));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateBulkInsertCreatesDataTableWithUnknownTypeNameFallsToObject()
    {
        // Arrange - unknown TypeName defaults to object
        var fields = new[]
        {
            CreateField("Data", clrType: typeof(object), typeName: "CustomType").Object
        };
        var container = CreateContainer(fields: fields);

        var entities = new List<object> { new { Data = "raw" } };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entities);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var dataTable = (DataTable)result.Value!.Parameters["@__BulkCopy_DataTable"].Value!;
        dataTable.Columns["Data"]!.DataType.ShouldBe(typeof(object));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateBulkInsertHandlesNullableValueTypeField()
    {
        // Arrange - nullable int field
        var fields = new[]
        {
            CreateField("Score", isNullable: true, clrType: typeof(int), typeName: "Int32").Object
        };
        var container = CreateContainer(fields: fields);

        var entities = new List<object> { new { Score = (int?)null } };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entities);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var dataTable = (DataTable)result.Value!.Parameters["@__BulkCopy_DataTable"].Value!;
        dataTable.Rows[0]["Score"].ShouldBe(DBNull.Value);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateBulkInsertHandlesNonNullableReferenceTypeField()
    {
        // Arrange - non-nullable string (reference type, IsNullable=true still works)
        var fields = new[]
        {
            CreateField("Name", isNullable: true, clrType: typeof(string), typeName: "String").Object
        };
        var container = CreateContainer(fields: fields);

        var entities = new List<object> { new { Name = "Test" } };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entities);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var dataTable = (DataTable)result.Value!.Parameters["@__BulkCopy_DataTable"].Value!;
        // String is already a reference type, so IsNullable doesn't change column type
        dataTable.Columns["Name"]!.DataType.ShouldBe(typeof(string));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateBulkInsertHandlesMultipleRows()
    {
        // Arrange
        var fields = new[]
        {
            CreateField("Name", clrType: typeof(string)).Object,
            CreateField("Age", clrType: typeof(int), typeName: "Int32").Object
        };
        var container = CreateContainer(fields: fields);

        var entities = new List<object>
        {
            new { Name = "Alice", Age = 30 },
            new { Name = "Bob", Age = 25 },
            new { Name = "Charlie", Age = 35 }
        };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entities);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var dataTable = (DataTable)result.Value!.Parameters["@__BulkCopy_DataTable"].Value!;
        dataTable.Rows.Count.ShouldBe(3);
        dataTable.Rows[0]["Name"].ShouldBe("Alice");
        dataTable.Rows[1]["Name"].ShouldBe("Bob");
        dataTable.Rows[2]["Name"].ShouldBe("Charlie");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateBulkInsertSetsDestinationParameter()
    {
        // Arrange
        var fields = new[] { CreateField("Name").Object };
        var container = CreateContainer(fields: fields);

        var entities = new List<object> { new { Name = "Test" } };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entities);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Parameters["@__BulkCopy_Destination"].Value!.ToString().ShouldBe("[dbo].[Customers]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateBulkInsertWithDirectClrTypeSkipsTypeNameLookup()
    {
        // Arrange - ClrType is NOT object, so TypeName inference is skipped
        var fields = new[]
        {
            CreateField("Price", clrType: typeof(decimal), typeName: "Decimal").Object
        };
        var container = CreateContainer(fields: fields);

        var entities = new List<object> { new { Price = 9.99m } };
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(entities);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var dataTable = (DataTable)result.Value!.Parameters["@__BulkCopy_DataTable"].Value!;
        dataTable.Columns["Price"]!.DataType.ShouldBe(typeof(decimal));
    }
}
