using Fdw.Data.RowSources.Http.Abstractions;

namespace Fdw.Data.RowSources.Http.Abstractions.Tests;

public class HttpRowEnumeratorOptionsTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void PageSizeDefaultsTo100()
    {
        var sut = new HttpRowEnumeratorOptions();

        sut.PageSize.ShouldBe(100);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void MaxPagesDefaultsToZero()
    {
        var sut = new HttpRowEnumeratorOptions();

        sut.MaxPages.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TimeoutSecondsDefaultsTo30()
    {
        var sut = new HttpRowEnumeratorOptions();

        sut.TimeoutSeconds.ShouldBe(30);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void JsonOptionsDefaultsToNewInstance()
    {
        var sut = new HttpRowEnumeratorOptions();

        sut.JsonOptions.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void InheritsRowSourceOptionsDefaults()
    {
        var sut = new HttpRowEnumeratorOptions();

        sut.BufferSize.ShouldBe(16 * 1024);
        sut.ContinueOnError.ShouldBeTrue();
        sut.MaxRows.ShouldBe(0);
        sut.MaxRowErrors.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void PageSizeCanBeSet()
    {
        var sut = new HttpRowEnumeratorOptions { PageSize = 50 };

        sut.PageSize.ShouldBe(50);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void MaxPagesCanBeSet()
    {
        var sut = new HttpRowEnumeratorOptions { MaxPages = 10 };

        sut.MaxPages.ShouldBe(10);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TimeoutSecondsCanBeSet()
    {
        var sut = new HttpRowEnumeratorOptions { TimeoutSeconds = 60 };

        sut.TimeoutSeconds.ShouldBe(60);
    }
}

public class RestStreamingOptionsTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void PaginationStyleDefaultsToOffsetLimit()
    {
        var sut = new RestStreamingOptions();

        sut.PaginationStyle.ShouldBe(RestPaginationStyles.OffsetLimit);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void OffsetParameterDefaultsToOffset()
    {
        var sut = new RestStreamingOptions();

        sut.OffsetParameter.ShouldBe("offset");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void LimitParameterDefaultsToLimit()
    {
        var sut = new RestStreamingOptions();

        sut.LimitParameter.ShouldBe("limit");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void PageParameterDefaultsToPage()
    {
        var sut = new RestStreamingOptions();

        sut.PageParameter.ShouldBe("page");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void CursorParameterDefaultsToCursor()
    {
        var sut = new RestStreamingOptions();

        sut.CursorParameter.ShouldBe("cursor");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void NextCursorPathDefaultsToNull()
    {
        var sut = new RestStreamingOptions();

        sut.NextCursorPath.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ParseLinkHeaderDefaultsToTrue()
    {
        var sut = new RestStreamingOptions();

        sut.ParseLinkHeader.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TotalCountPathDefaultsToNull()
    {
        var sut = new RestStreamingOptions();

        sut.TotalCountPath.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void InheritsHttpRowEnumeratorOptionsDefaults()
    {
        var sut = new RestStreamingOptions();

        sut.PageSize.ShouldBe(100);
        sut.TimeoutSeconds.ShouldBe(30);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void PaginationStyleCanBeSetToCursor()
    {
        var sut = new RestStreamingOptions { PaginationStyle = RestPaginationStyles.Cursor };

        sut.PaginationStyle.ShouldBe(RestPaginationStyles.Cursor);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void PaginationStyleCanBeSetToPageNumber()
    {
        var sut = new RestStreamingOptions { PaginationStyle = RestPaginationStyles.PageNumber };

        sut.PaginationStyle.ShouldBe(RestPaginationStyles.PageNumber);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void PaginationStyleCanBeSetToLinkHeader()
    {
        var sut = new RestStreamingOptions { PaginationStyle = RestPaginationStyles.LinkHeader };

        sut.PaginationStyle.ShouldBe(RestPaginationStyles.LinkHeader);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void NextCursorPathCanBeSet()
    {
        var sut = new RestStreamingOptions { NextCursorPath = "$.meta.next_cursor" };

        sut.NextCursorPath.ShouldBe("$.meta.next_cursor");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ParseLinkHeaderCanBeDisabled()
    {
        var sut = new RestStreamingOptions { ParseLinkHeader = false };

        sut.ParseLinkHeader.ShouldBeFalse();
    }
}

public class ODataStreamingOptionsTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void RequestCountDefaultsToTrue()
    {
        var sut = new ODataStreamingOptions();

        sut.RequestCount.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void SelectDefaultsToNull()
    {
        var sut = new ODataStreamingOptions();

        sut.Select.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void FilterDefaultsToNull()
    {
        var sut = new ODataStreamingOptions();

        sut.Filter.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void OrderByDefaultsToNull()
    {
        var sut = new ODataStreamingOptions();

        sut.OrderBy.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ExpandDefaultsToNull()
    {
        var sut = new ODataStreamingOptions();

        sut.Expand.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void InheritsHttpRowEnumeratorOptionsDefaults()
    {
        var sut = new ODataStreamingOptions();

        sut.PageSize.ShouldBe(100);
        sut.TimeoutSeconds.ShouldBe(30);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void RequestCountCanBeDisabled()
    {
        var sut = new ODataStreamingOptions { RequestCount = false };

        sut.RequestCount.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void SelectCanBeSet()
    {
        var sut = new ODataStreamingOptions { Select = "Name,Age" };

        sut.Select.ShouldBe("Name,Age");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void FilterCanBeSet()
    {
        var sut = new ODataStreamingOptions { Filter = "Age gt 18" };

        sut.Filter.ShouldBe("Age gt 18");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void OrderByCanBeSet()
    {
        var sut = new ODataStreamingOptions { OrderBy = "Name asc" };

        sut.OrderBy.ShouldBe("Name asc");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ExpandCanBeSet()
    {
        var sut = new ODataStreamingOptions { Expand = "Orders" };

        sut.Expand.ShouldBe("Orders");
    }
}

public class GraphQlStreamingOptionsTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void QueryTemplateDefaultsToNull()
    {
        var sut = new GraphQlStreamingOptions();

        sut.QueryTemplate.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TypeNameDefaultsToNull()
    {
        var sut = new GraphQlStreamingOptions();

        sut.TypeName.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void FieldSelectionDefaultsToNull()
    {
        var sut = new GraphQlStreamingOptions();

        sut.FieldSelection.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void EdgesPathDefaultsToNull()
    {
        var sut = new GraphQlStreamingOptions();

        sut.EdgesPath.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void PageInfoPathDefaultsToNull()
    {
        var sut = new GraphQlStreamingOptions();

        sut.PageInfoPath.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void InheritsHttpRowEnumeratorOptionsDefaults()
    {
        var sut = new GraphQlStreamingOptions();

        sut.PageSize.ShouldBe(100);
        sut.TimeoutSeconds.ShouldBe(30);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void QueryTemplateCanBeSet()
    {
        var sut = new GraphQlStreamingOptions
        {
            QueryTemplate = "query { users(first: {first}, after: {after}) { edges { node { id } } } }"
        };

        sut.QueryTemplate.ShouldContain("users");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TypeNameCanBeSet()
    {
        var sut = new GraphQlStreamingOptions { TypeName = "User" };

        sut.TypeName.ShouldBe("User");
    }
}

public class RestPaginationStyleTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void OffsetLimitIsDefault()
    {
        RestPaginationStyles.OffsetLimit.ShouldNotBeNull();
        RestPaginationStyles.OffsetLimit.Name.ShouldBe("OffsetLimit");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void HasFourValues()
    {
        RestPaginationStyles.All().Count.ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ContainsPageNumber()
    {
        RestPaginationStyles.PageNumber.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ContainsCursor()
    {
        RestPaginationStyles.Cursor.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ContainsLinkHeader()
    {
        RestPaginationStyles.LinkHeader.ShouldNotBeNull();
    }
}
