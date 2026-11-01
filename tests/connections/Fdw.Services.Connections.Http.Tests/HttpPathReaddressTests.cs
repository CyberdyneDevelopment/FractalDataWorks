using Fdw.Data.Http.Paths;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Tests;

/// <summary>
/// FDW-1985: an HTTP path is read at another address by replacing its path (the whole request path, query string included) and/or its last
/// segment; a part not stated is kept. This is how dates worked out earlier in a run reach the request.
/// </summary>
public sealed class HttpPathReaddressTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AStatedPathReplacesTheWholeRequestPathQueryStringIncluded()
    {
        var moved = new HttpPath("v1/data").Readdress("v1/data?from=2026-10-19&to=2026-10-20", null).ShouldBeOfType<HttpPath>();

        moved.PathValue.ShouldBe("v1/data?from=2026-10-19&to=2026-10-20");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AStatedNameReplacesTheLastSegmentAndKeepsTheQueryString()
    {
        var moved = new HttpPath("v1/data/today?limit=50").Readdress(null, "yesterday").ShouldBeOfType<HttpPath>();

        moved.PathValue.ShouldBe("v1/data/yesterday?limit=50");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ANameWithNoSlashInThePathBecomesTheWholeRoute()
    {
        new HttpPath("teams").Readdress(null, "scores").ShouldBeOfType<HttpPath>().PathValue.ShouldBe("scores");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AStatedPathAndNameApplyTogetherTheNameToTheNewPath()
    {
        var moved = new HttpPath("v1/data").Readdress("v2/orders/today?x=1", "yesterday").ShouldBeOfType<HttpPath>();

        moved.PathValue.ShouldBe("v2/orders/yesterday?x=1");
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", " ")]
    public void BlankPartsKeepTheAddressAndTheOriginalPathIsNotChanged(string? pathPart, string? namePart)
    {
        var original = new HttpPath("v1/data?from=a");

        var same = original.Readdress(pathPart, namePart).ShouldBeOfType<HttpPath>();

        same.PathValue.ShouldBe("v1/data?from=a");
        original.PathValue.ShouldBe("v1/data?from=a");
    }
}
