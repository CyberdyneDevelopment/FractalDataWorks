using System.Collections.Generic;
using System.Text.Json;
using Fdw.WebMcp.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.WebMcp.Mcp.Tests;

/// <summary>
/// What an agent's arguments turn into. Asserted on the built request rather than by sending one,
/// so the rules are pinned without a server in the loop.
/// </summary>
public class WebMcpRequestBuilderTests
{
    private static Dictionary<string, JsonElement> Args(string json)
        => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    private static WebMcpToolRequest Build(WebMcpToolDescriptor tool, string json)
    {
        var built = WebMcpRequestBuilder.Build(
            WebMcpToolPlan.Create(tool, NullLogger.Instance), Args(json), NullLogger.Instance);

        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
        return built.Value!;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void APathValueIsEncodedIntoTheUrlRatherThanSubstitutedRaw()
    {
        // A value carrying '/' would reshape the path it landed in.
        Build(BridgeCatalogue.GetItem, """{"Name":"a/b c"}""")
            .RelativeUrl.ShouldBe("/items/a%2Fb%20c");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AMissingRequiredValueIsRefusedAndNothingIsSent()
    {
        var built = WebMcpRequestBuilder.Build(
            WebMcpToolPlan.Create(BridgeCatalogue.GetItem, NullLogger.Instance),
            Args("""{}"""),
            NullLogger.Instance);

        built.IsSuccess.ShouldBeFalse();
        built.Messages[0].Message.ShouldContain("Name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void OnlySuppliedFiltersReachTheQueryString()
    {
        // An omitted filter must widen the result, not narrow it to the empty string.
        Build(BridgeCatalogue.ListItems, """{"Take":5,"ReferenceId":""}""")
            .RelativeUrl.ShouldBe("/items?Take=5");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ABooleanTravelsAsJsonNotAsDotNetToString()
    {
        // JsonElement.ToString() renders this as "True"; the browser sends "true".
        Build(BridgeCatalogue.ListItems, """{"IncludeRetired":true}""")
            .RelativeUrl.ShouldBe("/items?IncludeRetired=true");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void APathValueIsNeverAlsoSentAsQuery()
    {
        Build(BridgeCatalogue.DeleteItem, """{"Name":"alpha"}""")
            .RelativeUrl.ShouldBe("/items/alpha");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ABodylessVerbCarriesNoBody()
    {
        Build(BridgeCatalogue.GetItem, """{"Name":"alpha"}""").Body.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ThePresetWinsOverWhateverTheAgentSent()
    {
        // The tool's own identity decides this value; an agent cannot talk it into another one.
        Build(BridgeCatalogue.CreateItem, """{"Name":"alpha","ServiceType":"PostgreSql"}""")
            .Body.ShouldBe("""{"Name":"alpha","ServiceType":"MsSql"}""");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TheMethodIsTheToolsOwnVerb()
    {
        Build(BridgeCatalogue.DeleteItem, """{"Name":"alpha"}""").Method.ShouldBe("DELETE");
    }
}
