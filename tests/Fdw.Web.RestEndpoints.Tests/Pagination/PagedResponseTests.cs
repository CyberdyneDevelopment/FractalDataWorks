using System.Collections.Generic;
using System.Linq;
using Fdw.Web.RestEndpoints.Pagination;

namespace Fdw.Web.RestEndpoints.Tests.Pagination;

public class PagedResponseTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var response = new PagedResponse<string>();

        // Assert
        response.Data.ShouldNotBeNull();
        response.Data.ShouldBeEmpty();
        response.Page.ShouldBe(0);
        response.PageSize.ShouldBe(0);
        response.TotalCount.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TotalPages_CalculatesCorrectly_WithExactDivision()
    {
        // Arrange
        var response = new PagedResponse<string>
        {
            PageSize = 10,
            TotalCount = 100
        };

        // Act
        var totalPages = response.TotalPages;

        // Assert
        totalPages.ShouldBe(10);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TotalPages_CalculatesCorrectly_WithRemainder()
    {
        // Arrange
        var response = new PagedResponse<string>
        {
            PageSize = 10,
            TotalCount = 95
        };

        // Act
        var totalPages = response.TotalPages;

        // Assert
        totalPages.ShouldBe(10);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TotalPages_ReturnsZero_WhenPageSizeIsZero()
    {
        // Arrange
        var response = new PagedResponse<string>
        {
            PageSize = 0,
            TotalCount = 100
        };

        // Act
        var totalPages = response.TotalPages;

        // Assert
        totalPages.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void HasPrevious_ReturnsTrue_WhenPageIsGreaterThanOne()
    {
        // Arrange
        var response = new PagedResponse<string> { Page = 2 };

        // Act
        var hasPrevious = response.HasPrevious;

        // Assert
        hasPrevious.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void HasPrevious_ReturnsFalse_WhenPageIsOne()
    {
        // Arrange
        var response = new PagedResponse<string> { Page = 1 };

        // Act
        var hasPrevious = response.HasPrevious;

        // Assert
        hasPrevious.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void HasNext_ReturnsTrue_WhenPageIsLessThanTotalPages()
    {
        // Arrange
        var response = new PagedResponse<string>
        {
            Page = 1,
            PageSize = 10,
            TotalCount = 100
        };

        // Act
        var hasNext = response.HasNext;

        // Assert
        hasNext.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void HasNext_ReturnsFalse_WhenPageEqualsToTotalPages()
    {
        // Arrange
        var response = new PagedResponse<string>
        {
            Page = 10,
            PageSize = 10,
            TotalCount = 100
        };

        // Act
        var hasNext = response.HasNext;

        // Assert
        hasNext.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void HasNext_ReturnsFalse_WhenPageIsGreaterThanTotalPages()
    {
        // Arrange
        var response = new PagedResponse<string>
        {
            Page = 11,
            PageSize = 10,
            TotalCount = 100
        };

        // Act
        var hasNext = response.HasNext;

        // Assert
        hasNext.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Count_ReturnsCorrectCount_WhenDataHasItems()
    {
        // Arrange
        var data = new List<string> { "item1", "item2", "item3" };
        var response = new PagedResponse<string> { Data = data };

        // Act
        var count = response.Count;

        // Assert
        count.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Count_ReturnsZero_WhenDataIsEmpty()
    {
        // Arrange
        var response = new PagedResponse<string> { Data = [] };

        // Act
        var count = response.Count;

        // Assert
        count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Count_ReturnsZero_WhenDataIsNull()
    {
        // Arrange
        var response = new PagedResponse<string> { Data = null! };

        // Act
        var count = response.Count;

        // Assert
        count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Create_WithParameters_SetsAllPropertiesCorrectly()
    {
        // Arrange
        var data = new List<string> { "item1", "item2", "item3" };
        var page = 2;
        var pageSize = 10;
        var totalCount = 50;

        // Act
        var response = PagedResponse<string>.Create(data, page, pageSize, totalCount);

        // Assert
        response.Data.ShouldBe(data);
        response.Page.ShouldBe(page);
        response.PageSize.ShouldBe(pageSize);
        response.TotalCount.ShouldBe(totalCount);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Create_WithPagedRequest_SetsAllPropertiesCorrectly()
    {
        // Arrange
        var data = new List<string> { "item1", "item2", "item3" };
        var request = new PagedRequest { Page = 3, PageSize = 25 };
        var totalCount = 100;

        // Act
        var response = PagedResponse<string>.Create(data, request, totalCount);

        // Assert
        response.Data.ShouldBe(data);
        response.Page.ShouldBe(3);
        response.PageSize.ShouldBe(25);
        response.TotalCount.ShouldBe(totalCount);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Properties_CanBeSet()
    {
        // Arrange
        var data = new List<string> { "test" };

        // Act
        var response = new PagedResponse<string>
        {
            Data = data,
            Page = 5,
            PageSize = 20,
            TotalCount = 200
        };

        // Assert
        response.Data.ShouldBe(data);
        response.Page.ShouldBe(5);
        response.PageSize.ShouldBe(20);
        response.TotalCount.ShouldBe(200);
    }
}
