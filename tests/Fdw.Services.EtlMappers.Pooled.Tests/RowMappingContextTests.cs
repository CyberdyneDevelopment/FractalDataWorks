using Fdw.Data.Abstractions;
using Fdw;
using Fdw.Services;
using Fdw.Services.EtlMappers;

namespace Fdw.Services.EtlMappers.Pooled.Tests;

public sealed class RowMappingContextTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void CreateSetsFieldOrdinalFromReader()
    {
        // Arrange
        var (reader, container) = CreateReaderAndContainer("Name", "Age");
        reader.Setup(r => r.GetOrdinal("Name")).Returns(0);
        reader.Setup(r => r.GetOrdinal("Age")).Returns(1);

        // Act
        var context = RowMappingContext.Create(reader.Object, container.Object);

        // Assert
        context.FieldCount.ShouldBe(2);
        context.FieldOrdinals[0].ShouldBe(0);
        context.FieldOrdinals[1].ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void CreateSetsFieldNames()
    {
        // Arrange
        var (reader, container) = CreateReaderAndContainer("Name", "Age");
        reader.Setup(r => r.GetOrdinal("Name")).Returns(0);
        reader.Setup(r => r.GetOrdinal("Age")).Returns(1);

        // Act
        var context = RowMappingContext.Create(reader.Object, container.Object);

        // Assert
        context.FieldNames[0].ShouldBe("Name");
        context.FieldNames[1].ShouldBe("Age");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void CreateSetsMinus1ForMissingFieldInReader()
    {
        // Arrange
        var (reader, container) = CreateReaderAndContainer("Name", "MissingField");
        reader.Setup(r => r.GetOrdinal("Name")).Returns(0);
        reader.Setup(r => r.GetOrdinal("MissingField")).Throws(
#pragma warning disable CA2201 // IDataReader.GetOrdinal is documented to throw this for an unknown name; the mock reproduces the contract
            new IndexOutOfRangeException()
#pragma warning restore CA2201
            );

        // Act
        var context = RowMappingContext.Create(reader.Object, container.Object);

        // Assert
        context.FieldOrdinals[0].ShouldBe(0);
        context.FieldOrdinals[1].ShouldBe(-1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void CreateWithEmptySchemaReturnsZeroFieldCount()
    {
        // Arrange
        var (reader, container) = CreateReaderAndContainer();

        // Act
        var context = RowMappingContext.Create(reader.Object, container.Object);

        // Assert
        context.FieldCount.ShouldBe(0);
        context.FieldOrdinals.ShouldBeEmpty();
        context.FieldNames.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void CreateFieldCountMatchesSchemaFieldCount()
    {
        // Arrange
        var (reader, container) = CreateReaderAndContainer("A", "B", "C");
        reader.Setup(r => r.GetOrdinal(It.IsAny<string>())).Returns(0);

        // Act
        var context = RowMappingContext.Create(reader.Object, container.Object);

        // Assert
        context.FieldCount.ShouldBe(3);
    }

    private static (Mock<IDataReader> reader, Mock<IStorageContainer> container) CreateReaderAndContainer(
        params string[] fieldNames)
    {
        var reader = new Mock<IDataReader>();
        var container = new Mock<IStorageContainer>();
        var schema = new Mock<IContainerSchema>();

        var fields = new List<IField>();
        foreach (var name in fieldNames)
        {
            var field = new Mock<IField>();
            field.Setup(f => f.Name).Returns(name);
            fields.Add(field.Object);
        }

        schema.Setup(s => s.Fields).Returns(fields);
        schema.Setup(s => s.GetProjectableFields()).Returns(fields);
        container.Setup(c => c.Schema).Returns(schema.Object);

        return (reader, container);
    }
}
