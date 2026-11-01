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
public class WebMcpMcpCatalogueTests
{
    private static WebMcpMcpCatalogue Catalogue(params WebMcpToolDescriptor[] tools)
        => new(new BridgeCatalogue.Registry(tools), NullLogger<WebMcpMcpCatalogue>.Instance);

    private static JsonElement Schema(WebMcpToolDescriptor tool)
        => Catalogue(tool).Tools().Single().InputSchema;

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TheSchemaOffersExactlyThePlansInputs()
    {
        var plan = WebMcpToolPlan.Create(BridgeCatalogue.ListItems, NullLogger.Instance);

        Schema(BridgeCatalogue.ListItems).GetProperty("properties")
            .EnumerateObject().Select(property => property.Name)
            .ShouldBe(plan.Inputs.Select(input => input.Name));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void OnlyPathParametersAreRequired()
    {
        Schema(BridgeCatalogue.GetItem).GetProperty("required")
            .EnumerateArray().Select(value => value.GetString()).ShouldBe(["Name"]);

        Schema(BridgeCatalogue.ListItems).GetProperty("required")
            .EnumerateArray().ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ThePresetFieldIsNeverOfferedToTheAgent()
    {
        // The tool fixes this value itself; asking for it would invite an agent to set it.
        Schema(BridgeCatalogue.CreateItem).GetProperty("properties")
            .EnumerateObject().Select(property => property.Name).ShouldBe(["Name", "Enabled"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TypesAndFormatsComeThroughAsJsonSchema()
    {
        var properties = Schema(BridgeCatalogue.ListItems).GetProperty("properties");

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
        Catalogue(BridgeCatalogue.GetItem).Tools().Single().Annotations!.ReadOnlyHint.ShouldBe(true);
        Catalogue(BridgeCatalogue.CreateItem).Tools().Single().Annotations.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EveryRegisteredToolIsOfferedAndNoneIsInvented()
    {
        Catalogue(BridgeCatalogue.ListItems, BridgeCatalogue.GetItem, BridgeCatalogue.CreateItem)
            .Tools().Select(tool => tool.Name)
            .ShouldBe(["list_items", "get_item", "create_item"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ANameThisHostDoesNotServeHasNoPlan()
    {
        Catalogue(BridgeCatalogue.ListItems).PlanFor("no_such_tool").ShouldBeNull();
    }
}
