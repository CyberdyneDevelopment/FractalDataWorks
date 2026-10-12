using Fdw.Web.RestEndpoints.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.RestEndpoints.Tests.Pagination;

/// <summary>
/// FDW-1194: a true-or-false query parameter is absent (what the caller says it is then), true, or false; anything else is
/// refused and never read as the default.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Api")]
public sealed class QueryFlagTests
{
    private static DefaultHttpContext Context(string query)
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString(query);
        return context;
    }

    [Theory]
    [InlineData("", false, false)]
    [InlineData("", true, true)]
    [InlineData("?paged=true", false, true)]
    [InlineData("?paged=false", true, false)]
    [InlineData("?paged=TRUE", false, true)]
    public void TheFlagIsWhatTheRequestSaysAndWhenAbsentWhatTheCallerStates(string query, bool whenAbsent, bool expected)
    {
        var flag = QueryFlag.Read(Context(query), "paged", NullLogger.Instance, whenAbsent);

        flag.IsSuccess.ShouldBeTrue();
        flag.Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData("?paged=yes")]
    [InlineData("?paged=1")]
    [InlineData("?paged=")]
    public void AValueThatIsNeitherTrueNorFalseIsRefused(string query)
    {
        QueryFlag.Read(Context(query), "paged", NullLogger.Instance).IsFailure.ShouldBeTrue();
    }
}
