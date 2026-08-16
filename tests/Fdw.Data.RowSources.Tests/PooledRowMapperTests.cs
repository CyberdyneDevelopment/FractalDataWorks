using Fdw.Data.Abstractions;
using Fdw.Data.RowSources;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Mappers;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.RowSources.Tests;

/// <summary>
/// Tests for the PooledRowMapper zero-allocation mapper.
/// </summary>
public class PooledRowMapperTests
{
    private static PooledRowMapper CreateMapper()
    {
        var logger = new Mock<ILogger<PooledRowMapper>>();
        return new PooledRowMapper(logger.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EstimatedAllocationsPerRowIsZero()
    {
        // Arrange
        var mapper = CreateMapper();

        // Assert
        mapper.EstimatedAllocationsPerRow.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsInitializedIsFalseBeforeInitialize()
    {
        // Arrange
        var mapper = CreateMapper();

        // Assert
        mapper.IsInitialized.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsInitializedIsTrueAfterInitializeWithSource()
    {
        // Arrange
        var mapper = CreateMapper();
        var container = CreateMockContainer(["Col1"]);
        var source = CreateMockRowSource(["Col1"]);

        // Act
        mapper.Initialize(source.Object, container.Object);

        // Assert
        mapper.IsInitialized.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MapRowReturnsFieldsFromSource()
    {
        // Arrange
        var mapper = CreateMapper();
        var container = CreateMockContainer(["Column1", "Column2"]);
        var source = CreateMockRowSource(["Column1", "Column2"]);
        source.Setup(s => s.GetValue(0)).Returns("Value1");
        source.Setup(s => s.GetValue(1)).Returns(42);
        mapper.Initialize(source.Object, container.Object);

        // Act
        var result = mapper.MapRow(source.Object);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result["Column1"].ShouldBe("Value1");
        result["Column2"].ShouldBe(42);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MapRowHandlesNullValues()
    {
        // Arrange
        var mapper = CreateMapper();
        var container = CreateMockContainer(["NullableColumn"]);
        var source = CreateMockRowSource(["NullableColumn"]);
        source.Setup(s => s.IsNull(0)).Returns(true);
        mapper.Initialize(source.Object, container.Object);

        // Act
        var result = mapper.MapRow(source.Object);

        // Assert
        result["NullableColumn"].ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ResetClearsState()
    {
        // Arrange
        var mapper = CreateMapper();
        var container = CreateMockContainer(["Col1"]);
        var source = CreateMockRowSource(["Col1"]);
        mapper.Initialize(source.Object, container.Object);

        // Act
        mapper.Reset();

        // Assert
        mapper.IsInitialized.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MapRowReturnsEmptyDictionaryWhenNotInitializedWithSource()
    {
        // Arrange
        var mapper = CreateMapper();
        var container = CreateMockContainer(["Col1"]);
        mapper.Initialize(container.Object); // Initialize without source

        var source = CreateMockRowSource(["Col1"]);

        // Act
        var result = mapper.MapRow(source.Object);

        // Assert - should return empty because context was not created with source
        result.ShouldNotBeNull();
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
