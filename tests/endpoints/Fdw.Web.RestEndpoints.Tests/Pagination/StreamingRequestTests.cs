using Fdw.Web.RestEndpoints.Pagination;

namespace Fdw.Web.RestEndpoints.Tests.Pagination;

public class StreamingRequestTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var request = new StreamingRequest();

        // Assert
        request.BatchSize.ShouldBe(1000);
        request.MaxItems.ShouldBeNull();
        request.Filter.ShouldBeNull();
        request.SortBy.ShouldBeNull();
        request.SortDirection.ShouldBe("asc");
        request.Format.ShouldBe("json");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void IsDescending_ReturnsFalse_WhenSortDirectionIsAsc()
    {
        // Arrange
        var request = new StreamingRequest { SortDirection = "asc" };

        // Act
        var isDescending = request.IsDescending;

        // Assert
        isDescending.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void IsDescending_ReturnsFalse_WhenSortDirectionIsAscUpperCase()
    {
        // Arrange
        var request = new StreamingRequest { SortDirection = "ASC" };

        // Act
        var isDescending = request.IsDescending;

        // Assert
        isDescending.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void IsDescending_ReturnsTrue_WhenSortDirectionIsDesc()
    {
        // Arrange
        var request = new StreamingRequest { SortDirection = "desc" };

        // Act
        var isDescending = request.IsDescending;

        // Assert
        isDescending.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void IsDescending_ReturnsTrue_WhenSortDirectionIsDescUpperCase()
    {
        // Arrange
        var request = new StreamingRequest { SortDirection = "DESC" };

        // Act
        var isDescending = request.IsDescending;

        // Assert
        isDescending.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void IsDescending_ReturnsTrue_WhenSortDirectionIsMixedCase()
    {
        // Arrange
        var request = new StreamingRequest { SortDirection = "DeSc" };

        // Act
        var isDescending = request.IsDescending;

        // Assert
        isDescending.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void IsDescending_ReturnsFalse_WhenSortDirectionIsNull()
    {
        // Arrange
        var request = new StreamingRequest { SortDirection = null! };

        // Act
        var isDescending = request.IsDescending;

        // Assert
        isDescending.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ShouldCompress_ReturnsTrue_WhenBatchSizeIsGreaterThan100()
    {
        // Arrange
        var request = new StreamingRequest { BatchSize = 101 };

        // Act
        var shouldCompress = request.ShouldCompress;

        // Assert
        shouldCompress.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ShouldCompress_ReturnsFalse_WhenBatchSizeIs100()
    {
        // Arrange
        var request = new StreamingRequest { BatchSize = 100 };

        // Act
        var shouldCompress = request.ShouldCompress;

        // Assert
        shouldCompress.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ShouldCompress_ReturnsTrue_WhenMaxItemsIsGreaterThan1000()
    {
        // Arrange
        var request = new StreamingRequest { BatchSize = 10, MaxItems = 1001 };

        // Act
        var shouldCompress = request.ShouldCompress;

        // Assert
        shouldCompress.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ShouldCompress_ReturnsFalse_WhenMaxItemsIs1000()
    {
        // Arrange
        var request = new StreamingRequest { BatchSize = 10, MaxItems = 1000 };

        // Act
        var shouldCompress = request.ShouldCompress;

        // Assert
        shouldCompress.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ShouldCompress_ReturnsFalse_WhenBothConditionsAreFalse()
    {
        // Arrange
        var request = new StreamingRequest { BatchSize = 50, MaxItems = 500 };

        // Act
        var shouldCompress = request.ShouldCompress;

        // Assert
        shouldCompress.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ShouldCompress_ReturnsFalse_WhenMaxItemsIsNull()
    {
        // Arrange
        var request = new StreamingRequest { BatchSize = 50, MaxItems = null };

        // Act
        var shouldCompress = request.ShouldCompress;

        // Assert
        shouldCompress.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Properties_CanBeSet()
    {
        // Arrange & Act
        var request = new StreamingRequest
        {
            BatchSize = 500,
            MaxItems = 5000,
            Filter = "test filter",
            SortBy = "Name",
            SortDirection = "desc",
            Format = "csv"
        };

        // Assert
        request.BatchSize.ShouldBe(500);
        request.MaxItems.ShouldBe(5000);
        request.Filter.ShouldBe("test filter");
        request.SortBy.ShouldBe("Name");
        request.SortDirection.ShouldBe("desc");
        request.Format.ShouldBe("csv");
    }
}
