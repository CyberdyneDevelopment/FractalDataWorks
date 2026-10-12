using Fdw.WebMcp.Abstractions;

namespace Fdw.WebMcp.Hosting.Tests;

/// <summary>The collection endpoint options declare themselves into.</summary>
public class DeclaredWebMcpToolsTests
{
    private sealed class OnlyDeclaredOnceEndpoint { }

    [Fact]
    public void DeclaringTheSameEndpointTwiceOffersOneTool()
    {
        // An option is reachable directly and through its collection, so it can register twice.
        // A tool offered twice is a duplicate name in the generated script.
        var before = DeclaredWebMcpTools.Count;

        DeclaredWebMcpTools.Declare(new WebMcpToolDeclaration(
            typeof(OnlyDeclaredOnceEndpoint), "once", "Declared twice, offered once.", ReadOnly: true, null));
        DeclaredWebMcpTools.Declare(new WebMcpToolDeclaration(
            typeof(OnlyDeclaredOnceEndpoint), "once", "Declared twice, offered once.", ReadOnly: true, null));

        (DeclaredWebMcpTools.Count - before).ShouldBe(1);
    }
}
