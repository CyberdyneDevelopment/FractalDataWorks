using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections.Http.Paging;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Tests.Paging;

/// <summary>FDW-4938: the next page is the link whose rel names next, not the first link the header lists.</summary>
[ExcludeFromCodeCoverage]
public sealed class LinkHeaderTests
{
    [Theory]
    [InlineData("<https://a.test/p1>; rel=\"prev\", <https://a.test/p3>; rel=\"next\", <https://a.test/p9>; rel=\"last\"", "https://a.test/p3")]
    [InlineData("<https://a.test/p3>; rel=\"next\", <https://a.test/p1>; rel=\"prev\"", "https://a.test/p3")]
    [InlineData("<https://a.test/p1>; rel=prev, <https://a.test/p3>; rel=next", "https://a.test/p3")]
    [InlineData("<https://a.test/p3>; rel=\"next last\"", "https://a.test/p3")]
    [InlineData("<https://a.test/p1>; title=\"a, b\"; rel=\"prev\", <https://a.test/p3>; REL=\"NEXT\"", "https://a.test/p3")]
    [InlineData("</items?page=3>; rel=\"next\"", "/items?page=3")]
    public void TheLinkMarkedNextIsTaken(string header, string expected)
        => LinkHeader.Target(header, "next").ShouldBe(expected);

    [Theory]
    [InlineData("<https://a.test/p1>; rel=\"prev\", <https://a.test/p9>; rel=\"last\"")]
    [InlineData("<https://a.test/p1>; rel=\"nextish\"")]
    [InlineData("")]
    public void AHeaderWithNoLinkMarkedNextHasNone(string header)
        => LinkHeader.Target(header, "next").ShouldBeNull();
}
