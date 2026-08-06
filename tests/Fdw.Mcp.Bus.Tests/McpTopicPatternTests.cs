namespace Fdw.Mcp.Bus.Tests;

public class McpTopicPatternTests
{
    [Theory]
    [InlineData("mssql/103/dbo.Orders/schema", "mssql/103/dbo.Orders/schema")]
    [InlineData("mssql/*/schema",              "mssql/103/schema")]
    [InlineData("mssql/**",                    "mssql/103/dbo.Orders/schema")]
    [InlineData("**",                          "anything/at/all")]
    [InlineData("a/**/z",                      "a/b/c/z")]
    [InlineData("a/**/z",                      "a/z")]
    public void MatchesPositiveCases(string pattern, string topic)
    {
        McpTopicPattern.Matches(pattern, topic).ShouldBeTrue();
    }

    [Theory]
    [InlineData("mssql/103/schema", "mssql/103/dbo.Orders/schema")]
    [InlineData("mssql/*/schema",   "mssql/103/dbo.Orders/schema")]
    [InlineData("a/b",              "a/b/c")]
    [InlineData("a/**/z",           "a/b/c")]
    public void MatchesNegativeCases(string pattern, string topic)
    {
        McpTopicPattern.Matches(pattern, topic).ShouldBeFalse();
    }
}
