using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections.Http.Paging;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Tests.Paging;

/// <summary>FDW-4938: a paging style writes only its own parameters, so every page keeps the query's filter and ordering.</summary>
[ExcludeFromCodeCoverage]
public sealed class HttpQueryTests
{
    [Theory]
    [InlineData("items", "items?offset=20")]
    [InlineData("items?filter=a&sort=b", "items?filter=a&sort=b&offset=20")]
    [InlineData("items?offset=10&filter=a", "items?filter=a&offset=20")]
    [InlineData("items?filter=a#top", "items?filter=a&offset=20#top")]
    public void AParameterIsSetAndEveryOtherPartIsKept(string request, string expected)
        => HttpQuery.With(new Uri(request, UriKind.Relative), ("offset", "20")).OriginalString.ShouldBe(expected);

    [Fact]
    public void AnAbsoluteAddressStaysAbsoluteAndAValueIsEscaped()
    {
        var page = HttpQuery.With(new Uri("https://a.test/items?x=1"), ("page[number]", "2"), ("cursor", "a b/c"));

        page.IsAbsoluteUri.ShouldBeTrue();
        page.OriginalString.ShouldBe("https://a.test/items?x=1&page[number]=2&cursor=a%20b%2Fc");
    }
}
