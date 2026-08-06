using Fdw.Data.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataContainers.Abstractions.Tests;

public class RuntimeDataSetConstructorTests
{
    private readonly Mock<IDataSchema> _mockSchema = new();

    public RuntimeDataSetConstructorTests()
    {
        _mockSchema.Setup(s => s.Id).Returns("test-schema");
        _mockSchema.Setup(s => s.Name).Returns("TestSchema");
        _mockSchema.Setup(s => s.Version).Returns("1.0");
        _mockSchema.Setup(s => s.Fields).Returns(new List<ISchemaField>());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SuccessThrowsWhenNameIsNull()
    {
        // Arrange
        var rows = new[] { Mock.Of<IDataRow>() };

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => RuntimeDataSet.Success(null!, _mockSchema.Object, rows));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SuccessThrowsWhenSchemaIsNull()
    {
        // Arrange
        var rows = new[] { Mock.Of<IDataRow>() };

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => RuntimeDataSet.Success("TestDataSet", null!, rows));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SuccessThrowsWhenRowsIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => RuntimeDataSet.Success("TestDataSet", _mockSchema.Object, null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EmptyThrowsWhenNameIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => RuntimeDataSet.Empty(null!, _mockSchema.Object));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EmptyThrowsWhenSchemaIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => RuntimeDataSet.Empty("TestDataSet", null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FromRowsThrowsWhenNameIsNull()
    {
        // Arrange
        var row = Mock.Of<IDataRow>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => RuntimeDataSet.FromRows(null!, _mockSchema.Object, row));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FromRowsThrowsWhenSchemaIsNull()
    {
        // Arrange
        var row = Mock.Of<IDataRow>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => RuntimeDataSet.FromRows("TestDataSet", null!, row));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FromRowsThrowsWhenRowsIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => RuntimeDataSet.FromRows("TestDataSet", _mockSchema.Object, null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FailureCreatesFailedDataSetWithNullName()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => RuntimeDataSet.Failure(null!, "Error message"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FailureAllowsNullErrorMessage()
    {
        // Act
        var dataset = RuntimeDataSet.Failure("TestDataSet", null!);

        // Assert
        dataset.IsSuccess.ShouldBeFalse();
        dataset.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FailureAllowsEmptyErrorMessage()
    {
        // Act
        var dataset = RuntimeDataSet.Failure("TestDataSet", string.Empty);

        // Assert
        dataset.IsSuccess.ShouldBeFalse();
        dataset.ErrorMessage.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SuccessWorksWithEmptyRowsCollection()
    {
        // Arrange
        var rows = Array.Empty<IDataRow>();

        // Act
        var dataset = RuntimeDataSet.Success("TestDataSet", _mockSchema.Object, rows);

        // Assert
        dataset.IsSuccess.ShouldBeTrue();
        dataset.RowCount.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FromRowsWorksWithNoRowParameters()
    {
        // Act
        var dataset = RuntimeDataSet.FromRows("TestDataSet", _mockSchema.Object);

        // Assert
        dataset.IsSuccess.ShouldBeTrue();
        dataset.RowCount.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FromRowsWorksWithSingleRow()
    {
        // Arrange
        var row = Mock.Of<IDataRow>();

        // Act
        var dataset = RuntimeDataSet.FromRows("TestDataSet", _mockSchema.Object, row);

        // Assert
        dataset.IsSuccess.ShouldBeTrue();
        dataset.RowCount.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FromRowsWorksWithMultipleRows()
    {
        // Arrange
        var row1 = Mock.Of<IDataRow>();
        var row2 = Mock.Of<IDataRow>();
        var row3 = Mock.Of<IDataRow>();

        // Act
        var dataset = RuntimeDataSet.FromRows("TestDataSet", _mockSchema.Object, row1, row2, row3);

        // Assert
        dataset.IsSuccess.ShouldBeTrue();
        dataset.RowCount.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SuccessPreservesSchemaReference()
    {
        // Arrange
        var rows = Array.Empty<IDataRow>();

        // Act
        var dataset = RuntimeDataSet.Success("TestDataSet", _mockSchema.Object, rows);

        // Assert
        dataset.Schema.ShouldBe(_mockSchema.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EmptyPreservesSchemaReference()
    {
        // Act
        var dataset = RuntimeDataSet.Empty("TestDataSet", _mockSchema.Object);

        // Assert
        dataset.Schema.ShouldBe(_mockSchema.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FailureHasEmptySchema()
    {
        // Act
        var dataset = RuntimeDataSet.Failure("TestDataSet", "Error");

        // Assert
        dataset.Schema.ShouldNotBeNull();
        dataset.Schema.Id.ShouldBe("empty");
    }
}
