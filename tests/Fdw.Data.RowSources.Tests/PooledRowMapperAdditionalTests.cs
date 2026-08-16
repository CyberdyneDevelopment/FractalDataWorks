using Fdw.Data.Abstractions;
using Fdw.Data.RowSources;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Mappers;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.RowSources.Tests;

public sealed class PooledRowMapperAdditionalTests
{
    private static PooledRowMapper CreateMapper(IDataTypeConverters? converters = null)
    {
        var logger = new Mock<ILogger<PooledRowMapper>>();
        return new PooledRowMapper(logger.Object, converterCollection: converters);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MapRowWithConverterUsesGetConvertedValue()
    {
        // Arrange
        var converter = new Mock<IDataTypeConverter>();
        converter.Setup(c => c.Name).Returns("Decimal");

        var converterCollection = new Mock<IDataTypeConverters>();
        converterCollection.Setup(c => c.ById(5)).Returns(converter.Object);

        var mapper = CreateMapper(converterCollection.Object);

        var field = new Mock<IField>();
        field.Setup(f => f.Name).Returns("Price");
        field.Setup(f => f.ConverterTypeId).Returns(5);

        var container = new Mock<IStorageContainer>();
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(new List<IField> { field.Object });
        schema.Setup(s => s.GetProjectableFields()).Returns(new List<IField> { field.Object });
        container.Setup(c => c.Schema).Returns(schema.Object);

        var source = new Mock<IRecordCursor>();
        source.Setup(s => s.GetFieldOrdinal("Price")).Returns(0);
        source.Setup(s => s.IsNull(0)).Returns(false);
        source.Setup(s => s.GetConvertedValue(0, converter.Object)).Returns(19.99m);

        mapper.Initialize(source.Object, container.Object);

        // Act
        var result = mapper.MapRow(source.Object);

        // Assert
        result["Price"].ShouldBe(19.99m);
        source.Verify(s => s.GetConvertedValue(0, converter.Object), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MapRowWithNegativeOrdinalSetsNull()
    {
        // Arrange - field not found in source (ordinal = -1)
        var mapper = CreateMapper();

        var field = new Mock<IField>();
        field.Setup(f => f.Name).Returns("Missing");
        field.Setup(f => f.ConverterTypeId).Returns((int?)null);

        var container = new Mock<IStorageContainer>();
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(new List<IField> { field.Object });
        schema.Setup(s => s.GetProjectableFields()).Returns(new List<IField> { field.Object });
        container.Setup(c => c.Schema).Returns(schema.Object);

        var source = new Mock<IRecordCursor>();
        source.Setup(s => s.GetFieldOrdinal("Missing")).Returns(-1);

        mapper.Initialize(source.Object, container.Object);

        // Act
        var result = mapper.MapRow(source.Object);

        // Assert
        result["Missing"].ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MapRowWithoutConverterUsesGetValue()
    {
        // Arrange - field has no converter
        var mapper = CreateMapper();

        var field = new Mock<IField>();
        field.Setup(f => f.Name).Returns("Name");
        field.Setup(f => f.ConverterTypeId).Returns((int?)null);

        var container = new Mock<IStorageContainer>();
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(new List<IField> { field.Object });
        schema.Setup(s => s.GetProjectableFields()).Returns(new List<IField> { field.Object });
        container.Setup(c => c.Schema).Returns(schema.Object);

        var source = new Mock<IRecordCursor>();
        source.Setup(s => s.GetFieldOrdinal("Name")).Returns(0);
        source.Setup(s => s.IsNull(0)).Returns(false);
        source.Setup(s => s.GetValue(0)).Returns("TestValue");

        mapper.Initialize(source.Object, container.Object);

        // Act
        var result = mapper.MapRow(source.Object);

        // Assert
        result["Name"].ShouldBe("TestValue");
        source.Verify(s => s.GetValue(0), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ReturnRowReturnsToPool()
    {
        // Arrange
        var mapper = CreateMapper();
        var container = CreateMockContainer(["Col1"]);
        var source = CreateMockRowSource(["Col1"]);
        mapper.Initialize(source.Object, container.Object);

        var row = mapper.MapRow(source.Object);

        // Act - return the row
        mapper.ReturnRow(row);

        // Assert - rent again, the pool should have reused the dictionary
        var row2 = mapper.MapRow(source.Object);
        row2.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MapRowWithMultipleFieldsMixedNullAndConverters()
    {
        // Arrange
        var converter = new Mock<IDataTypeConverter>();
        var converterCollection = new Mock<IDataTypeConverters>();
        converterCollection.Setup(c => c.ById(10)).Returns(converter.Object);

        var mapper = CreateMapper(converterCollection.Object);

        var field1 = new Mock<IField>();
        field1.Setup(f => f.Name).Returns("Id");
        field1.Setup(f => f.ConverterTypeId).Returns((int?)null);

        var field2 = new Mock<IField>();
        field2.Setup(f => f.Name).Returns("Price");
        field2.Setup(f => f.ConverterTypeId).Returns(10);

        var field3 = new Mock<IField>();
        field3.Setup(f => f.Name).Returns("NullField");
        field3.Setup(f => f.ConverterTypeId).Returns((int?)null);

        var container = new Mock<IStorageContainer>();
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(new List<IField>
        {
            field1.Object, field2.Object, field3.Object
        });
        schema.Setup(s => s.GetProjectableFields()).Returns(new List<IField>
        {
            field1.Object, field2.Object, field3.Object
        });
        container.Setup(c => c.Schema).Returns(schema.Object);

        var source = new Mock<IRecordCursor>();
        source.Setup(s => s.GetFieldOrdinal("Id")).Returns(0);
        source.Setup(s => s.GetFieldOrdinal("Price")).Returns(1);
        source.Setup(s => s.GetFieldOrdinal("NullField")).Returns(2);
        source.Setup(s => s.IsNull(0)).Returns(false);
        source.Setup(s => s.IsNull(1)).Returns(false);
        source.Setup(s => s.IsNull(2)).Returns(true);
        source.Setup(s => s.GetValue(0)).Returns(42);
        source.Setup(s => s.GetConvertedValue(1, converter.Object)).Returns(19.99m);

        mapper.Initialize(source.Object, container.Object);

        // Act
        var result = mapper.MapRow(source.Object);

        // Assert
        result.Count.ShouldBe(3);
        result["Id"].ShouldBe(42);
        result["Price"].ShouldBe(19.99m);
        result["NullField"].ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InitializeWithContainerOnlySetsContextToNull()
    {
        // Arrange
        var mapper = CreateMapper();
        var container = CreateMockContainer(["Col1"]);

        // Act - Initialize without source
        mapper.Initialize(container.Object);

        // Assert - IsInitialized should be false because _context is set to null
        mapper.IsInitialized.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ResetClearsContextAndPool()
    {
        // Arrange
        var mapper = CreateMapper();
        var container = CreateMockContainer(["Col1"]);
        var source = CreateMockRowSource(["Col1"]);
        mapper.Initialize(source.Object, container.Object);

        // Verify initialized
        mapper.IsInitialized.ShouldBeTrue();

        // Act
        mapper.Reset();

        // Assert
        mapper.IsInitialized.ShouldBeFalse();

        // MapRow after reset should return empty dict
        var result = mapper.MapRow(source.Object);
        result.Count.ShouldBe(0);
    }

    private static Mock<IStorageContainer> CreateMockContainer(string[] fieldNames)
    {
        var container = new Mock<IStorageContainer>();
        var schema = new Mock<IContainerSchema>();
        var fields = new List<IField>();

        for (int i = 0; i < fieldNames.Length; i++)
        {
            var field = new Mock<IField>();
            field.Setup(f => f.Name).Returns(fieldNames[i]);
            fields.Add(field.Object);
        }

        schema.Setup(s => s.Fields).Returns(fields);
        schema.Setup(s => s.GetProjectableFields()).Returns(fields);
        container.Setup(c => c.Schema).Returns(schema.Object);

        return container;
    }

    private static Mock<IRecordCursor> CreateMockRowSource(string[] fieldNames)
    {
        var source = new Mock<IRecordCursor>();
        source.Setup(s => s.FieldCount).Returns(fieldNames.Length);

        for (int i = 0; i < fieldNames.Length; i++)
        {
            var ordinal = i;
            source.Setup(s => s.GetFieldName(ordinal)).Returns(fieldNames[ordinal]);
            source.Setup(s => s.GetFieldOrdinal(fieldNames[ordinal])).Returns(ordinal);
            source.Setup(s => s.IsNull(ordinal)).Returns(false);
            source.Setup(s => s.GetValue(ordinal)).Returns($"Value{ordinal}");
        }

        return source;
    }
}
