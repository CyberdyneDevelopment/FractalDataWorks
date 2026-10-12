using Fdw.Data.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataContainers.Abstractions.Tests;

public class RuntimeDataSetTests
{
    private readonly Mock<IDataSchema> _mockSchema = new();

    public RuntimeDataSetTests()
    {
        _mockSchema.Setup(s => s.Id).Returns("test-schema");
        _mockSchema.Setup(s => s.Name).Returns("TestSchema");
        _mockSchema.Setup(s => s.Version).Returns("1.0");
        _mockSchema.Setup(s => s.Fields).Returns(new List<ISchemaField>());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EmptyCreatesSuccessfulEmptyDataSet()
    {
        // Arrange & Act
        var dataset = RuntimeDataSet.Empty("TestDataSet", _mockSchema.Object);

        // Assert
        dataset.IsSuccess.ShouldBeTrue();
        dataset.Name.ShouldBe("TestDataSet");
        dataset.Schema.ShouldBe(_mockSchema.Object);
        dataset.RowCount.ShouldBe(0);
        dataset.Rows.ShouldBeEmpty();
        dataset.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FromRowsCreatesSuccessfulDataSetWithRows()
    {
        // Arrange
        var row1 = Mock.Of<IDataRow>();
        var row2 = Mock.Of<IDataRow>();

        // Act
        var dataset = RuntimeDataSet.FromRows("TestDataSet", _mockSchema.Object, row1, row2);

        // Assert
        dataset.IsSuccess.ShouldBeTrue();
        dataset.RowCount.ShouldBe(2);
        dataset.Rows.Count().ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SuccessCreatesSuccessfulDataSet()
    {
        // Arrange
        var rows = new[] { Mock.Of<IDataRow>(), Mock.Of<IDataRow>() };

        // Act
        var dataset = RuntimeDataSet.Success("TestDataSet", _mockSchema.Object, rows);

        // Assert
        dataset.IsSuccess.ShouldBeTrue();
        dataset.RowCount.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FailureCreatesFailedDataSet()
    {
        // Arrange & Act
        var dataset = RuntimeDataSet.Failure("TestDataSet", "Something went wrong");

        // Assert
        dataset.IsSuccess.ShouldBeFalse();
        dataset.ErrorMessage.ShouldBe("Something went wrong");
        dataset.RowCount.ShouldBe(0);
        dataset.Rows.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetRowReturnsCorrectRowByIndex()
    {
        // Arrange
        var row1 = Mock.Of<IDataRow>();
        var row2 = Mock.Of<IDataRow>();
        var dataset = RuntimeDataSet.FromRows("TestDataSet", _mockSchema.Object, row1, row2);

        // Act
        var result = dataset.GetRow(1);

        // Assert
        result.ShouldBe(row2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetRowThrowsForNegativeIndex()
    {
        // Arrange
        var dataset = RuntimeDataSet.FromRows("TestDataSet", _mockSchema.Object, Mock.Of<IDataRow>());

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => dataset.GetRow(-1));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetRowThrowsForIndexOutOfRange()
    {
        // Arrange
        var dataset = RuntimeDataSet.FromRows("TestDataSet", _mockSchema.Object, Mock.Of<IDataRow>());

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => dataset.GetRow(10));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetRowThrowsOnFailedDataSet()
    {
        // Arrange
        var dataset = RuntimeDataSet.Failure("TestDataSet", "Error");

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => dataset.GetRow(0));
        ex.Message.ShouldContain("Cannot access rows on failed dataset");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WhereFiltersRowsCorrectly()
    {
        // Arrange
        var row1 = new Mock<IDataRow>();
        row1.Setup(r => r.GetValue<int>("Id")).Returns(1);
        var row2 = new Mock<IDataRow>();
        row2.Setup(r => r.GetValue<int>("Id")).Returns(2);
        var dataset = RuntimeDataSet.FromRows("TestDataSet", _mockSchema.Object, row1.Object, row2.Object);

        // Act
        var filtered = dataset.Where(r => r.GetValue<int>("Id") > 1);

        // Assert
        filtered.IsSuccess.ShouldBeTrue();
        filtered.RowCount.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WhereOnFailedDataSetReturnsSelf()
    {
        // Arrange
        var dataset = RuntimeDataSet.Failure("TestDataSet", "Error");

        // Act
        var filtered = dataset.Where(r => true);

        // Assert
        filtered.ShouldBe(dataset);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SelectTransformsRowsCorrectly()
    {
        // Arrange
        var row1 = Mock.Of<IDataRow>();
        var row2 = Mock.Of<IDataRow>();
        var dataset = RuntimeDataSet.FromRows("TestDataSet", _mockSchema.Object, row1, row2);

        // Act
        var transformed = dataset.Select(r => r);

        // Assert
        transformed.IsSuccess.ShouldBeTrue();
        transformed.RowCount.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SelectOnFailedDataSetReturnsSelf()
    {
        // Arrange
        var dataset = RuntimeDataSet.Failure("TestDataSet", "Error");

        // Act
        var transformed = dataset.Select(r => r);

        // Assert
        transformed.ShouldBe(dataset);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SumCalculatesCorrectTotal()
    {
        // Arrange
        var row1 = new Mock<IDataRow>();
        row1.Setup(r => r.GetValue<decimal>("Amount")).Returns(10.5m);
        var row2 = new Mock<IDataRow>();
        row2.Setup(r => r.GetValue<decimal>("Amount")).Returns(20.3m);
        var dataset = RuntimeDataSet.FromRows("TestDataSet", _mockSchema.Object, row1.Object, row2.Object);

        // Act
        var sum = dataset.Sum("Amount");

        // Assert
        sum.ShouldBe(30.8m);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SumThrowsOnFailedDataSet()
    {
        // Arrange
        var dataset = RuntimeDataSet.Failure("TestDataSet", "Error");

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => dataset.Sum("Amount"));
        ex.Message.ShouldContain("Cannot sum failed dataset");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AverageCalculatesCorrectAverage()
    {
        // Arrange
        var row1 = new Mock<IDataRow>();
        row1.Setup(r => r.GetValue<decimal>("Amount")).Returns(10m);
        var row2 = new Mock<IDataRow>();
        row2.Setup(r => r.GetValue<decimal>("Amount")).Returns(20m);
        var dataset = RuntimeDataSet.FromRows("TestDataSet", _mockSchema.Object, row1.Object, row2.Object);

        // Act
        var avg = dataset.Average("Amount");

        // Assert
        avg.ShouldBe(15m);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AverageThrowsOnFailedDataSet()
    {
        // Arrange
        var dataset = RuntimeDataSet.Failure("TestDataSet", "Error");

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => dataset.Average("Amount"));
        ex.Message.ShouldContain("Cannot average failed dataset");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MinReturnsMinimumValue()
    {
        // Arrange
        var row1 = new Mock<IDataRow>();
        row1.Setup(r => r.GetValue<decimal>("Amount")).Returns(10m);
        var row2 = new Mock<IDataRow>();
        row2.Setup(r => r.GetValue<decimal>("Amount")).Returns(5m);
        var dataset = RuntimeDataSet.FromRows("TestDataSet", _mockSchema.Object, row1.Object, row2.Object);

        // Act
        var min = dataset.Min("Amount");

        // Assert
        min.ShouldBe(5m);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MinThrowsOnFailedDataSet()
    {
        // Arrange
        var dataset = RuntimeDataSet.Failure("TestDataSet", "Error");

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => dataset.Min("Amount"));
        ex.Message.ShouldContain("Cannot find min on failed dataset");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MaxReturnsMaximumValue()
    {
        // Arrange
        var row1 = new Mock<IDataRow>();
        row1.Setup(r => r.GetValue<decimal>("Amount")).Returns(10m);
        var row2 = new Mock<IDataRow>();
        row2.Setup(r => r.GetValue<decimal>("Amount")).Returns(25m);
        var dataset = RuntimeDataSet.FromRows("TestDataSet", _mockSchema.Object, row1.Object, row2.Object);

        // Act
        var max = dataset.Max("Amount");

        // Assert
        max.ShouldBe(25m);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MaxThrowsOnFailedDataSet()
    {
        // Arrange
        var dataset = RuntimeDataSet.Failure("TestDataSet", "Error");

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => dataset.Max("Amount"));
        ex.Message.ShouldContain("Cannot find max on failed dataset");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CountReturnsCorrectCount()
    {
        // Arrange
        var dataset = RuntimeDataSet.FromRows("TestDataSet", _mockSchema.Object,
            Mock.Of<IDataRow>(), Mock.Of<IDataRow>(), Mock.Of<IDataRow>());

        // Act
        var count = dataset.Count();

        // Assert
        count.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CountWithPredicateFiltersCorrectly()
    {
        // Arrange
        var row1 = new Mock<IDataRow>();
        row1.Setup(r => r.GetValue<int>("Value")).Returns(5);
        var row2 = new Mock<IDataRow>();
        row2.Setup(r => r.GetValue<int>("Value")).Returns(10);
        var dataset = RuntimeDataSet.FromRows("TestDataSet", _mockSchema.Object, row1.Object, row2.Object);

        // Act
        var count = dataset.Count(r => r.GetValue<int>("Value") > 7);

        // Assert
        count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CountOnFailedDataSetReturnsZero()
    {
        // Arrange
        var dataset = RuntimeDataSet.Failure("TestDataSet", "Error");

        // Act
        var count = dataset.Count();

        // Assert
        count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GroupByGroupsRowsByFieldValue()
    {
        // Arrange
        var row1 = new Mock<IDataRow>();
        row1.Setup(r => r.GetValue("Category")).Returns("A");
        var row2 = new Mock<IDataRow>();
        row2.Setup(r => r.GetValue("Category")).Returns("B");
        var row3 = new Mock<IDataRow>();
        row3.Setup(r => r.GetValue("Category")).Returns("A");
        var dataset = RuntimeDataSet.FromRows("TestDataSet", _mockSchema.Object, row1.Object, row2.Object, row3.Object);

        // Act
        var groups = dataset.GroupBy("Category").ToList();

        // Assert
        groups.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GroupByOnFailedDataSetReturnsEmpty()
    {
        // Arrange
        var dataset = RuntimeDataSet.Failure("TestDataSet", "Error");

        // Act
        var groups = dataset.GroupBy("Category");

        // Assert
        groups.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OrderByAscendingSortsCorrectly()
    {
        // Arrange
        var row1 = new Mock<IDataRow>();
        row1.Setup(r => r.GetValue("Value")).Returns(3);
        var row2 = new Mock<IDataRow>();
        row2.Setup(r => r.GetValue("Value")).Returns(1);
        var row3 = new Mock<IDataRow>();
        row3.Setup(r => r.GetValue("Value")).Returns(2);
        var dataset = RuntimeDataSet.FromRows("TestDataSet", _mockSchema.Object, row1.Object, row2.Object, row3.Object);

        // Act
        var sorted = dataset.OrderBy("Value");

        // Assert
        sorted.IsSuccess.ShouldBeTrue();
        sorted.RowCount.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OrderByDescendingSortsCorrectly()
    {
        // Arrange
        var row1 = new Mock<IDataRow>();
        row1.Setup(r => r.GetValue("Value")).Returns(1);
        var row2 = new Mock<IDataRow>();
        row2.Setup(r => r.GetValue("Value")).Returns(3);
        var dataset = RuntimeDataSet.FromRows("TestDataSet", _mockSchema.Object, row1.Object, row2.Object);

        // Act
        var sorted = dataset.OrderBy("Value", descending: true);

        // Assert
        sorted.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OrderByOnFailedDataSetReturnsSelf()
    {
        // Arrange
        var dataset = RuntimeDataSet.Failure("TestDataSet", "Error");

        // Act
        var sorted = dataset.OrderBy("Value");

        // Assert
        sorted.ShouldBe(dataset);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TakeLimitsRowCount()
    {
        // Arrange
        var dataset = RuntimeDataSet.FromRows("TestDataSet", _mockSchema.Object,
            Mock.Of<IDataRow>(), Mock.Of<IDataRow>(), Mock.Of<IDataRow>());

        // Act
        var limited = dataset.Take(2);

        // Assert
        limited.RowCount.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TakeOnFailedDataSetReturnsSelf()
    {
        // Arrange
        var dataset = RuntimeDataSet.Failure("TestDataSet", "Error");

        // Act
        var limited = dataset.Take(5);

        // Assert
        limited.ShouldBe(dataset);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SkipSkipsCorrectNumberOfRows()
    {
        // Arrange
        var dataset = RuntimeDataSet.FromRows("TestDataSet", _mockSchema.Object,
            Mock.Of<IDataRow>(), Mock.Of<IDataRow>(), Mock.Of<IDataRow>());

        // Act
        var skipped = dataset.Skip(1);

        // Assert
        skipped.RowCount.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SkipOnFailedDataSetReturnsSelf()
    {
        // Arrange
        var dataset = RuntimeDataSet.Failure("TestDataSet", "Error");

        // Act
        var skipped = dataset.Skip(2);

        // Assert
        skipped.ShouldBe(dataset);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FirstOrDefaultReturnsFirstRow()
    {
        // Arrange
        var row1 = Mock.Of<IDataRow>();
        var row2 = Mock.Of<IDataRow>();
        var dataset = RuntimeDataSet.FromRows("TestDataSet", _mockSchema.Object, row1, row2);

        // Act
        var first = dataset.FirstOrDefault();

        // Assert
        first.ShouldBe(row1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FirstOrDefaultReturnsNullForEmptyDataSet()
    {
        // Arrange
        var dataset = RuntimeDataSet.Empty("TestDataSet", _mockSchema.Object);

        // Act
        var first = dataset.FirstOrDefault();

        // Assert
        first.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FirstOrDefaultWithPredicateReturnsMatchingRow()
    {
        // Arrange
        var row1 = new Mock<IDataRow>();
        row1.Setup(r => r.GetValue<int>("Id")).Returns(1);
        var row2 = new Mock<IDataRow>();
        row2.Setup(r => r.GetValue<int>("Id")).Returns(2);
        var dataset = RuntimeDataSet.FromRows("TestDataSet", _mockSchema.Object, row1.Object, row2.Object);

        // Act
        var result = dataset.FirstOrDefault(r => r.GetValue<int>("Id") == 2);

        // Assert
        result.ShouldBe(row2.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FirstOrDefaultOnFailedDataSetReturnsNull()
    {
        // Arrange
        var dataset = RuntimeDataSet.Failure("TestDataSet", "Error");

        // Act
        var first = dataset.FirstOrDefault();

        // Assert
        first.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToListReturnsListOfRows()
    {
        // Arrange
        var row1 = Mock.Of<IDataRow>();
        var row2 = Mock.Of<IDataRow>();
        var dataset = RuntimeDataSet.FromRows("TestDataSet", _mockSchema.Object, row1, row2);

        // Act
        var list = dataset.ToList();

        // Assert
        list.Count.ShouldBe(2);
        list[0].ShouldBe(row1);
        list[1].ShouldBe(row2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToListOnFailedDataSetReturnsEmptyList()
    {
        // Arrange
        var dataset = RuntimeDataSet.Failure("TestDataSet", "Error");

        // Act
        var list = dataset.ToList();

        // Assert
        list.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToArrayReturnsArrayOfRows()
    {
        // Arrange
        var row1 = Mock.Of<IDataRow>();
        var row2 = Mock.Of<IDataRow>();
        var dataset = RuntimeDataSet.FromRows("TestDataSet", _mockSchema.Object, row1, row2);

        // Act
        var array = dataset.ToArray();

        // Assert
        array.Length.ShouldBe(2);
        array[0].ShouldBe(row1);
        array[1].ShouldBe(row2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToArrayOnFailedDataSetReturnsEmptyArray()
    {
        // Arrange
        var dataset = RuntimeDataSet.Failure("TestDataSet", "Error");

        // Act
        var array = dataset.ToArray();

        // Assert
        array.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CountWithPredicateOnFailedDataSetReturnsZero()
    {
        // Arrange
        var dataset = RuntimeDataSet.Failure("TestDataSet", "Error");

        // Act
        var count = dataset.Count(row => true);

        // Assert
        count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FirstOrDefaultWithPredicateOnFailedDataSetReturnsNull()
    {
        // Arrange
        var dataset = RuntimeDataSet.Failure("TestDataSet", "Error");

        // Act
        var result = dataset.FirstOrDefault(row => true);

        // Assert
        result.ShouldBeNull();
    }
}
