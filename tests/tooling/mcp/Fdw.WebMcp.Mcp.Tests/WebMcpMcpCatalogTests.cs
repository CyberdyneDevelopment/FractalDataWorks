using System.Linq;
using System.Text.Json;
using Fdw.WebMcp.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.WebMcp.Mcp.Tests;

/// <summary>
/// How this host's tools are described to a headless agent.
/// </summary>
/// <remarks>
/// Both transports are pinned to the per-tool plan: the browser script by a golden test over the
/// whole generated file, and MCP by these. The plan is one object, so agreement between them is a
/// property of the design rather than of two descriptions happening to match.
/// </remarks>
public class WebMcpMcpCatalogTests
{
    private static WebMcpMcpCatalog Catalog(params WebMcpToolDescriptor[] tools)
        => new(new BridgeCatalog.Registry(tools), NullLogger<WebMcpMcpCatalog>.Instance);

    private static JsonElement Schema(WebMcpToolDescriptor tool)
        => Catalog(tool).Tools().Single().InputSchema;

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TheSchemaOffersExactlyThePlansInputs()
    {
        var plan = Catalog(BridgeCatalog.ListItems).PlanFor(BridgeCatalog.ListItems.Name).ShouldNotBeNull();

        Schema(BridgeCatalog.ListItems).GetProperty("properties")
            .EnumerateObject().Select(property => property.Name)
            .ShouldBe(plan.Inputs.Select(input => input.Name));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AListAndAnObjectAreDescribedWholeAndWhatTheyRequireIsListed()
    {
        var schema = Schema(BridgeCatalog.CreateMeeting);
        var properties = schema.GetProperty("properties");

        properties.GetProperty("Recipients").GetProperty("type").GetString().ShouldBe("array");
        properties.GetProperty("Recipients").GetProperty("items").GetProperty("type").GetString().ShouldBe("string");

        var attendee = properties.GetProperty("Attendees").GetProperty("items");
        attendee.GetProperty("type").GetString().ShouldBe("object");
        attendee.GetProperty("properties").EnumerateObject().Select(member => member.Name).ShouldBe(["Email", "Seats"]);
        attendee.GetProperty("required").EnumerateArray().Select(value => value.GetString()).ShouldBe(["Email", "Seats"]);

        schema.GetProperty("required").EnumerateArray().Select(value => value.GetString()).ShouldBe(["Attendees"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AScalarOnlyToolsSchemaCarriesNoItemsOrNestedRequired()
    {
        // Extend, never change: a tool with only scalars is described exactly as it was.
        Schema(BridgeCatalog.ListItems).GetProperty("properties").GetProperty("Take").EnumerateObject()
            .Select(member => member.Name).ShouldBe(["type"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void OnlyPathParametersAreRequired()
    {
        Schema(BridgeCatalog.GetItem).GetProperty("required")
            .EnumerateArray().Select(value => value.GetString()).ShouldBe(["Name"]);

        Schema(BridgeCatalog.ListItems).GetProperty("required")
            .EnumerateArray().ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ThePresetFieldIsNeverOfferedToTheAgent()
    {
        // The tool fixes this value itself; asking for it would invite an agent to set it.
        Schema(BridgeCatalog.CreateItem).GetProperty("properties")
            .EnumerateObject().Select(property => property.Name).ShouldBe(["Name", "Enabled"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TypesAndFormatsComeThroughAsJsonSchema()
    {
        var properties = Schema(BridgeCatalog.ListItems).GetProperty("properties");

        properties.GetProperty("ReferenceId").GetProperty("type").GetString().ShouldBe("string");
        properties.GetProperty("Take").GetProperty("type").GetString().ShouldBe("integer");
        properties.GetProperty("IncludeRetired").GetProperty("type").GetString().ShouldBe("boolean");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ReadOnlyIsAnnotatedOnlyWhenTheToolDeclaredIt()
    {
        // An absent hint and a false one do not mean the same thing to a client.
        Catalog(BridgeCatalog.GetItem).Tools().Single().Annotations!.ReadOnlyHint.ShouldBe(true);
        Catalog(BridgeCatalog.CreateItem).Tools().Single().Annotations.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EveryRegisteredToolIsOfferedAndNoneIsInvented()
    {
        Catalog(BridgeCatalog.ListItems, BridgeCatalog.GetItem, BridgeCatalog.CreateItem)
            .Tools().Select(tool => tool.Name)
            .ShouldBe(["list_items", "get_item", "create_item"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ANameThisHostDoesNotServeHasNoPlan()
    {
        Catalog(BridgeCatalog.ListItems).PlanFor("no_such_tool").ShouldBeNull();
    }
}
