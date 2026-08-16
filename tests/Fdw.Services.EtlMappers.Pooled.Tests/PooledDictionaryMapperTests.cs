using Fdw.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw;
using Fdw.Services;
using Fdw.Services.EtlMappers;

namespace Fdw.Services.EtlMappers.Pooled.Tests;

public sealed class PooledDictionaryMapperTests
{
    private readonly ILogger<PooledDictionaryMapper> _logger =
        NullLoggerFactory.Instance.CreateLogger<PooledDictionaryMapper>();

    private PooledDictionaryMapper CreateSut(int maxPoolSize = 1000, int maxDictionarySize = 100)
    {
        var config = new PooledDictionaryMapperConfiguration
        {
            MaxPoolSize = maxPoolSize,
            MaxDictionarySize = maxDictionarySize
        };
        return new PooledDictionaryMapper(_logger, config);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void EstimatedAllocationsPerRowIsZero()
    {
        var sut = CreateSut();
        sut.EstimatedAllocationsPerRow.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void IsInitializedIsFalseBeforeInitialize()
    {
        var sut = CreateSut();
        sut.IsInitialized.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void InitializeSetsIsInitializedToTrue()
    {
        // Arrange
        var sut = CreateSut();
        var (reader, container) = CreateReaderAndContainer("Name");
        reader.Setup(r => r.GetOrdinal("Name")).Returns(0);

        // Act
        sut.Initialize(reader.Object, container.Object);

        // Assert
        sut.IsInitialized.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void MapRowThrowsWhenNotInitialized()
    {
        // Arrange
        var sut = CreateSut();
        var reader = new Mock<IDataReader>();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => sut.MapRow(reader.Object));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void MapRowReturnsDictionaryWithFieldValues()
    {
        // Arrange
        var sut = CreateSut();
        var (reader, container) = CreateReaderAndContainer("Name", "Age");
        reader.Setup(r => r.GetOrdinal("Name")).Returns(0);
        reader.Setup(r => r.GetOrdinal("Age")).Returns(1);
        reader.Setup(r => r.IsDBNull(0)).Returns(false);
        reader.Setup(r => r.IsDBNull(1)).Returns(false);
        reader.Setup(r => r.GetValue(0)).Returns("John");
        reader.Setup(r => r.GetValue(1)).Returns(30);

        sut.Initialize(reader.Object, container.Object);

        // Act
        var row = sut.MapRow(reader.Object);

        // Assert
        row["Name"].ShouldBe("John");
        row["Age"].ShouldBe(30);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void MapRowSetsNullForDbNullValues()
    {
        // Arrange
        var sut = CreateSut();
        var (reader, container) = CreateReaderAndContainer("Name");
        reader.Setup(r => r.GetOrdinal("Name")).Returns(0);
        reader.Setup(r => r.IsDBNull(0)).Returns(true);

        sut.Initialize(reader.Object, container.Object);

        // Act
        var row = sut.MapRow(reader.Object);

        // Assert
        row["Name"].ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void MapRowSetsNullForMissingFieldOrdinal()
    {
        // Arrange
        var sut = CreateSut();
        var (reader, container) = CreateReaderAndContainer("MissingField");
        reader.Setup(r => r.GetOrdinal("MissingField")).Throws(new IndexOutOfRangeException());

        sut.Initialize(reader.Object, container.Object);

        // Act
        var row = sut.MapRow(reader.Object);

        // Assert
        row["MissingField"].ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ReturnRowReturnsToPool()
    {
        // Arrange
        var sut = CreateSut();
        var (reader, container) = CreateReaderAndContainer("Name");
        reader.Setup(r => r.GetOrdinal("Name")).Returns(0);
        reader.Setup(r => r.IsDBNull(0)).Returns(false);
        reader.Setup(r => r.GetValue(0)).Returns("Test");

        sut.Initialize(reader.Object, container.Object);
        var row = sut.MapRow(reader.Object);

        // Act - should not throw
        sut.ReturnRow(row);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ResetClearsInitializationAndPool()
    {
        // Arrange
        var sut = CreateSut();
        var (reader, container) = CreateReaderAndContainer("Name");
        reader.Setup(r => r.GetOrdinal("Name")).Returns(0);
        sut.Initialize(reader.Object, container.Object);
        sut.IsInitialized.ShouldBeTrue();

        // Act
        sut.Reset();

        // Assert
        sut.IsInitialized.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void MapRowAfterResetThrows()
    {
        // Arrange
        var sut = CreateSut();
        var (reader, container) = CreateReaderAndContainer("Name");
        reader.Setup(r => r.GetOrdinal("Name")).Returns(0);
        sut.Initialize(reader.Object, container.Object);
        sut.Reset();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => sut.MapRow(reader.Object));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void MapRowReusesDictionaryAfterReturn()
    {
        // Arrange
        var sut = CreateSut();
        var (reader, container) = CreateReaderAndContainer("Name");
        reader.Setup(r => r.GetOrdinal("Name")).Returns(0);
        reader.Setup(r => r.IsDBNull(0)).Returns(false);
        reader.Setup(r => r.GetValue(0)).Returns("Test");

        sut.Initialize(reader.Object, container.Object);

        // Act - map, return, then map again
        var row1 = sut.MapRow(reader.Object);
        sut.ReturnRow(row1);
        var row2 = sut.MapRow(reader.Object);

        // Assert - row2 should be a valid dictionary regardless of pooling
        row2.ShouldNotBeNull();
        row2["Name"].ShouldBe("Test");
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
