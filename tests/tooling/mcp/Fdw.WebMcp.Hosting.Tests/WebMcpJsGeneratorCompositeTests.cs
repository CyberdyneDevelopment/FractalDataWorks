using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.WebMcp.Hosting.Tests;

/// <summary>
/// The script tells a browser agent the whole request of a tool (FDW-6143): lists, objects and what each requires.
/// </summary>
public class WebMcpJsGeneratorCompositeTests
{
    private static string Script(params WebMcpToolDescriptor[] tools)
        => new WebMcpJsGenerator(
                new WebMcpGoldenCatalog.Registry(tools),
                NullLogger<WebMcpJsGenerator>.Instance)
            .Generate(new WebMcpHostSettings("X-Webmcp-Key", ClientApiKey: null, SelfAddressSource: null, SelfBaseAddress: null, SchemaMaxDepth: WebMcpCompositeCatalog.MaxDepth))
            .ReplaceLineEndings("\n");

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ATool_WithListsAndObjects_IsDescribedWholeInItsInputSchema()
    {
        Script(WebMcpCompositeCatalog.Meeting).ShouldContain("""
    inputSchema: {
      type: "object",
      properties: {
        "Title": { "type": "string" },
        "Recipients": { "type": "array", "items": { "type": "string" } },
        "Attendees": { "type": "array", "items": { "type": "object", "properties": { "Email": { "type": "string" }, "Seats": { "type": "integer" }, "Note": { "type": "string" } }, "required": ["Email", "Seats"] } },
        "Recurrence": { "type": "object", "properties": { "Pattern": { "type": "string" }, "Count": { "type": "integer" } }, "required": ["Count"] }
      },
      required: ["Attendees"]
    },
""".ReplaceLineEndings("\n"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ARequiredCompositeMemberIsListedBesideThePathParameters()
    {
        Script(WebMcpCompositeCatalog.MeetingByRoom).ShouldContain("""required: ["Room"]""");
    }
}
