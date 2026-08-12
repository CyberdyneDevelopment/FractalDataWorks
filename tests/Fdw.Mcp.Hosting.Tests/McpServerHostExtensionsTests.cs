using System;
using System.Linq;
using Fdw.Mcp.Hosting.SampleTools;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Fdw.Mcp.Hosting.Tests;

/// <summary>
/// Verifies the composition mechanism: a tool declared by a referenced package reaches the server
/// without any registration call naming it. Nothing here constructs
/// <c>SampleEchoToolType</c> — the project reference is the whole wiring.
/// </summary>
public class McpServerHostExtensionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void McpToolTypesContainsToolDeclaredByReferencedPackage()
    {
        McpToolTypes.All()
            .Any(t => string.Equals(t.Name, "SampleEcho", StringComparison.Ordinal))
            .ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToolTypeExposesItsToolClass()
    {
        McpToolTypes.All()
            .First(t => string.Equals(t.Name, "SampleEcho", StringComparison.Ordinal))
            .ToolClass
            .ShouldBe(typeof(SampleEchoTool));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AddFdwMcpServerComposesDeclaredToolsAndSucceeds()
    {
        var result = new ServiceCollection().AddFdwMcpServer();

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }
}
