using Fdw.Data.RowSources.Http.Abstractions;

namespace Fdw.Data.RowSources.Tests;

public sealed class HttpRowEnumeratorOptionsTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void DefaultPageSizeIs100()
    {
        var options = new HttpRowEnumeratorOptions();

        options.PageSize.ShouldBe(100);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void DefaultMaxPagesIsZero()
    {
        var options = new HttpRowEnumeratorOptions();

        options.MaxPages.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void DefaultTimeoutIs30Seconds()
    {
        var options = new HttpRowEnumeratorOptions();

        options.TimeoutSeconds.ShouldBe(30);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void JsonOptionsIsNotNull()
    {
        var options = new HttpRowEnumeratorOptions();

        options.JsonOptions.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void RestStreamingOptionsDefaults()
    {
        var options = new RestStreamingOptions();

        options.PaginationStyle.ShouldBe(RestPaginationStyles.OffsetLimit);
        options.OffsetParameter.ShouldBe("offset");
        options.LimitParameter.ShouldBe("limit");
        options.PageParameter.ShouldBe("page");
        options.CursorParameter.ShouldBe("cursor");
        options.NextCursorPath.ShouldBeNull();
        options.ParseLinkHeader.ShouldBeTrue();
        options.TotalCountPath.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ODataStreamingOptionsDefaults()
    {
        var options = new ODataStreamingOptions();

        options.RequestCount.ShouldBeTrue();
        options.Select.ShouldBeNull();
        options.Filter.ShouldBeNull();
        options.OrderBy.ShouldBeNull();
        options.Expand.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void GraphQlStreamingOptionsDefaults()
    {
        var options = new GraphQlStreamingOptions();

        options.QueryTemplate.ShouldBeNull();
        options.TypeName.ShouldBeNull();
        options.FieldSelection.ShouldBeNull();
        options.EdgesPath.ShouldBeNull();
        options.PageInfoPath.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void RestStreamingOptionsCanSetProperties()
    {
        var options = new RestStreamingOptions
        {
            PaginationStyle = RestPaginationStyles.Cursor,
            CursorParameter = "next",
            NextCursorPath = "$.meta.cursor",
            ParseLinkHeader = false,
            PageSize = 50
        };

        options.PaginationStyle.ShouldBe(RestPaginationStyles.Cursor);
        options.CursorParameter.ShouldBe("next");
        options.NextCursorPath.ShouldBe("$.meta.cursor");
        options.ParseLinkHeader.ShouldBeFalse();
        options.PageSize.ShouldBe(50);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ODataStreamingOptionsCanSetProperties()
    {
        var options = new ODataStreamingOptions
        {
            RequestCount = false,
            Select = "Name,Id",
            Filter = "Status eq 'Active'",
            OrderBy = "Name asc",
            Expand = "Details"
        };

        options.RequestCount.ShouldBeFalse();
        options.Select.ShouldBe("Name,Id");
        options.Filter.ShouldBe("Status eq 'Active'");
        options.OrderBy.ShouldBe("Name asc");
        options.Expand.ShouldBe("Details");
    }
}
