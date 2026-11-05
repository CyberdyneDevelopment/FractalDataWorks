using System.Collections.Generic;
using Fdw.Web.RestEndpoints.Pagination;
using Microsoft.AspNetCore.Http;

namespace Fdw.Web.RestEndpoints.Tests.Pagination;

/// <summary>
/// FDW-3816: a list reads the tags it is asked for from <c>?tag=</c> through <see cref="TagQuery"/>, repeated or comma separated, so every list that
/// can be filtered by tag reads them the same way.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Api")]
public sealed class TagQueryTests
{
    private static IReadOnlyList<string> Read(string query)
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString(query);
        return TagQuery.Read(context);
    }

    [Fact]
    public void NoQueryNamesNoTag() => Read(string.Empty).ShouldBeEmpty();

    [Fact]
    public void ARepeatedValueNamesEveryTag() => Read("?tag=finance&tag=q3").ShouldBe(["finance", "q3"]);

    [Fact]
    public void ACommaSeparatedValueNamesEveryTag() => Read("?tag=finance,q3").ShouldBe(["finance", "q3"]);

    [Fact]
    public void ABlankValueNamesNoTag() => Read("?tag=%20,").ShouldBeEmpty();

    [Fact]
    public void ATagIsNamedOnceWhateverItsCase() => Read("?tag=Finance&tag=finance").ShouldBe(["Finance"]);

    [Fact]
    public void SpacesAroundATagAreNotPartOfIt() => Read("?tag=%20finance%20,%20q3").ShouldBe(["finance", "q3"]);
}
