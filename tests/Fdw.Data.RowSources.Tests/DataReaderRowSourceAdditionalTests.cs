using System.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.DataReader.Abstractions;

namespace Fdw.Data.RowSources.Tests;

public class DataReaderRowSourceAdditionalTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CanResetReturnsFalse()
    {
        // Arrange
        var reader = CreateMockReader(["Col1"]);
        using var source = new DataReaderRowSource(reader.Object);

        // Assert
        source.CanReset.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ResetIsNoOp()
    {
        // Arrange
        var reader = CreateMockReader(["Col1"]);
        using var source = new DataReaderRowSource(reader.Object);

        // Act & Assert - should not throw
        Should.NotThrow(() => source.Reset());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldNameReturnsEmptyForNegativeOrdinal()
    {
        // Arrange
        var reader = CreateMockReader(["Col1"]);
        using var source = new DataReaderRowSource(reader.Object);

        // Act
        var name = source.GetFieldName(-1);

        // Assert
        name.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldNameReturnsEmptyForOutOfRangeOrdinal()
    {
        // Arrange
        var reader = CreateMockReader(["Col1"]);
        using var source = new DataReaderRowSource(reader.Object);

        // Act
        var name = source.GetFieldName(99);

        // Assert
        name.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldOrdinalReturnsMinusOneForNullFieldName()
    {
        // Arrange
        var reader = CreateMockReader(["Col1"]);
        using var source = new DataReaderRowSource(reader.Object);

        // Act
        var ordinal = source.GetFieldOrdinal(null!);

        // Assert
        ordinal.ShouldBe(-1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldOrdinalReturnsMinusOneForEmptyFieldName()
    {
        // Arrange
        var reader = CreateMockReader(["Col1"]);
        using var source = new DataReaderRowSource(reader.Object);

        // Act
        var ordinal = source.GetFieldOrdinal("");

        // Assert
        ordinal.ShouldBe(-1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldOrdinalReturnsMinusOneForUnknownFieldName()
    {
        // Arrange
        var reader = CreateMockReader(["Col1", "Col2"]);
        using var source = new DataReaderRowSource(reader.Object);

        // Act
        var ordinal = source.GetFieldOrdinal("NonExistentColumn");

        // Assert
        ordinal.ShouldBe(-1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldOrdinalIsCaseInsensitive()
    {
        // Arrange
        var reader = CreateMockReader(["MyColumn"]);
        using var source = new DataReaderRowSource(reader.Object);

        // Act
        var ordinal = source.GetFieldOrdinal("mycolumn");

        // Assert
        ordinal.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsNullReturnsTrueForNegativeOrdinal()
    {
        // Arrange
        var reader = CreateMockReader(["Col1"]);
        using var source = new DataReaderRowSource(reader.Object);

        // Act
        var isNull = source.IsNull(-1);

        // Assert
        isNull.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsNullReturnsTrueForOutOfRangeOrdinal()
    {
        // Arrange
        var reader = CreateMockReader(["Col1"]);
        using var source = new DataReaderRowSource(reader.Object);

        // Act
        var isNull = source.IsNull(99);

        // Assert
        isNull.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueReturnsNullForNegativeOrdinal()
    {
        // Arrange
        var reader = CreateMockReader(["Col1"]);
        using var source = new DataReaderRowSource(reader.Object);

        // Act
        var value = source.GetValue(-1);

        // Assert
        value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueReturnsNullForOutOfRangeOrdinal()
    {
        // Arrange
        var reader = CreateMockReader(["Col1"]);
        using var source = new DataReaderRowSource(reader.Object);

        // Act
        var value = source.GetValue(99);

        // Assert
        value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueReturnsNullForDbNull()
    {
        // Arrange
        var reader = CreateMockReader(["Col1"]);
        reader.Setup(r => r.IsDBNull(0)).Returns(true);
        using var source = new DataReaderRowSource(reader.Object);
        source.Read();

        // Act
        var value = source.GetValue(0);

        // Assert
        value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetConvertedValueReturnsNullWhenRawValueIsNull()
    {
        // Arrange
        var reader = CreateMockReader(["Col1"]);
        reader.Setup(r => r.IsDBNull(0)).Returns(true);
        var converter = new Mock<IDataTypeConverter>();
        using var source = new DataReaderRowSource(reader.Object);
        source.Read();

        // Act
        var value = source.GetConvertedValue(0, converter.Object);

        // Assert
        value.ShouldBeNull();
        converter.Verify(c => c.ToClr(It.IsAny<object>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetConvertedValueCallsConverterWhenValueExists()
    {
        // Arrange
        var reader = CreateMockReader(["Col1"]);
        reader.Setup(r => r.IsDBNull(0)).Returns(false);
        reader.Setup(r => r.GetValue(0)).Returns("raw-value");
        var converter = new Mock<IDataTypeConverter>();
        converter.Setup(c => c.ToClr("raw-value")).Returns("converted-value");
        using var source = new DataReaderRowSource(reader.Object);
        source.Read();

        // Act
        var value = source.GetConvertedValue(0, converter.Object);

        // Assert
        value.ShouldBe("converted-value");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DoubleDisposeDoesNotThrow()
    {
        // Arrange
        var reader = CreateMockReader(["Col1"]);
        var source = new DataReaderRowSource(reader.Object);

        // Act
        source.Dispose();
        Should.NotThrow(() => source.Dispose());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DuplicateColumnNamesKeepsFirstOrdinal()
    {
        // Arrange - simulate reader with duplicate column names
        var reader = new Mock<IDataReader>();
        reader.Setup(r => r.FieldCount).Returns(3);
        reader.Setup(r => r.GetName(0)).Returns("Col1");
        reader.Setup(r => r.GetName(1)).Returns("Col1"); // duplicate
        reader.Setup(r => r.GetName(2)).Returns("Col2");
        reader.SetupSequence(r => r.Read()).Returns(true).Returns(false);

        using var source = new DataReaderRowSource(reader.Object);

        // Act - should keep ordinal 0 for "Col1"
        var ordinal = source.GetFieldOrdinal("Col1");

        // Assert
        ordinal.ShouldBe(0);
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
