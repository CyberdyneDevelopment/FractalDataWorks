using System.Data;
using Fdw.Data.RowSources.DataReader.Abstractions;

namespace Fdw.Data.RowSources.Tests;

/// <summary>
/// Tests for the DataReaderRowSource adapter.
/// </summary>
public class DataReaderRowSourceTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenReaderIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new DataReaderRowSource(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FieldCountReturnsReaderFieldCount()
    {
        // Arrange
        var reader = CreateMockReader(["Col1", "Col2", "Col3"]);
        using var source = new DataReaderRowSource(reader.Object);

        // Assert
        source.FieldCount.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldNameReturnsCorrectName()
    {
        // Arrange
        var reader = CreateMockReader(["Column1", "Column2"]);
        using var source = new DataReaderRowSource(reader.Object);

        // Assert
        source.GetFieldName(0).ShouldBe("Column1");
        source.GetFieldName(1).ShouldBe("Column2");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldOrdinalReturnsCorrectOrdinal()
    {
        // Arrange
        var reader = CreateMockReader(["Column1", "Column2"]);
        reader.Setup(r => r.GetOrdinal("Column1")).Returns(0);
        reader.Setup(r => r.GetOrdinal("Column2")).Returns(1);
        using var source = new DataReaderRowSource(reader.Object);

        // Assert
        source.GetFieldOrdinal("Column1").ShouldBe(0);
        source.GetFieldOrdinal("Column2").ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsNullReturnsCorrectValue()
    {
        // Arrange
        var reader = CreateMockReader(["Col1", "Col2"]);
        reader.Setup(r => r.IsDBNull(0)).Returns(false);
        reader.Setup(r => r.IsDBNull(1)).Returns(true);
        using var source = new DataReaderRowSource(reader.Object);
        source.Read(); // Need to read first

        // Assert
        source.IsNull(0).ShouldBeFalse();
        source.IsNull(1).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueReturnsCorrectValue()
    {
        // Arrange
        var reader = CreateMockReader(["Col1", "Col2"]);
        reader.Setup(r => r.GetValue(0)).Returns("Test");
        reader.Setup(r => r.GetValue(1)).Returns(42);
        using var source = new DataReaderRowSource(reader.Object);
        source.Read();

        // Assert
        source.GetValue(0).ShouldBe("Test");
        source.GetValue(1).ShouldBe(42);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ReadReturnsReaderReadResult()
    {
        // Arrange
        var reader = CreateMockReader(["Col1"]);
        reader.SetupSequence(r => r.Read())
            .Returns(true)
            .Returns(true)
            .Returns(false);
        using var source = new DataReaderRowSource(reader.Object);

        // Act & Assert
        source.Read().ShouldBeTrue();
        source.HasCurrentRow.ShouldBeTrue();
        source.Read().ShouldBeTrue();
        source.HasCurrentRow.ShouldBeTrue();
        source.Read().ShouldBeFalse();
        source.HasCurrentRow.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void HasCurrentRowIsFalseBeforeRead()
    {
        // Arrange
        var reader = CreateMockReader(["Col1"]);
        using var source = new DataReaderRowSource(reader.Object);

        // Assert
        source.HasCurrentRow.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EstimatedAllocationsPerRowIsZero()
    {
        // Arrange
        var reader = CreateMockReader(["Col1"]);
        using var source = new DataReaderRowSource(reader.Object);

        // Assert
        source.EstimatedAllocationsPerRow.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DisposeDoesNotDisposeReader()
    {
        // Arrange - DataReaderRowSource does NOT own the reader; caller is responsible for disposal
        var reader = CreateMockReader(["Col1"]);
        var source = new DataReaderRowSource(reader.Object);

        // Act
        source.Dispose();

        // Assert - reader should NOT be disposed by the source
        reader.Verify(r => r.Dispose(), Times.Never);
    }

    private static Mock<IDataReader> CreateMockReader(string[] columns)
    {
        var reader = new Mock<IDataReader>();
        reader.Setup(r => r.FieldCount).Returns(columns.Length);
        for (int i = 0; i < columns.Length; i++)
        {
            var ordinal = i;
            reader.Setup(r => r.GetName(ordinal)).Returns(columns[ordinal]);
        }
        reader.SetupSequence(r => r.Read()).Returns(true).Returns(false);
        return reader;
    }
}
