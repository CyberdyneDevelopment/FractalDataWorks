using Fdw.Web.RestEndpoints.Pagination;

namespace Fdw.Web.RestEndpoints.Tests.Pagination;

public class PagedRequestTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var request = new PagedRequest();

        // Assert
        request.Page.ShouldBe(1);
        request.PageSize.ShouldBe(50);
        request.SortBy.ShouldBeNull();
        request.SortDirection.ShouldBe("asc");
        request.Search.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Offset_CalculatesCorrectly_ForFirstPage()
    {
        // Arrange
        var request = new PagedRequest { Page = 1, PageSize = 10 };

        // Act
        var offset = request.Offset;

        // Assert
        offset.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Offset_CalculatesCorrectly_ForSecondPage()
    {
        // Arrange
        var request = new PagedRequest { Page = 2, PageSize = 10 };

        // Act
        var offset = request.Offset;

        // Assert
        offset.ShouldBe(10);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Offset_CalculatesCorrectly_ForThirdPage()
    {
        // Arrange
        var request = new PagedRequest { Page = 3, PageSize = 25 };

        // Act
        var offset = request.Offset;

        // Assert
        offset.ShouldBe(50);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void IsDescending_ReturnsFalse_WhenSortDirectionIsAsc()
    {
        // Arrange
        var request = new PagedRequest { SortDirection = "asc" };

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
        var request = new PagedRequest { SortDirection = "ASC" };

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
        var request = new PagedRequest { SortDirection = "desc" };

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
        var request = new PagedRequest { SortDirection = "DESC" };

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
        var request = new PagedRequest { SortDirection = "DeSc" };

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
        var request = new PagedRequest { SortDirection = null! };

        // Act
        var isDescending = request.IsDescending;

        // Assert
        isDescending.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Properties_CanBeSet()
    {
        // Arrange & Act
        var request = new PagedRequest
        {
            Page = 5,
            PageSize = 100,
            SortBy = "Name",
            SortDirection = "desc",
            Search = "test"
        };

        // Assert
        request.Page.ShouldBe(5);
        request.PageSize.ShouldBe(100);
        request.SortBy.ShouldBe("Name");
        request.SortDirection.ShouldBe("desc");
        request.Search.ShouldBe("test");
    }
}
