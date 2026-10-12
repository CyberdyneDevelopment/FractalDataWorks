using System;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.WebMcp.Hosting.Tests;

/// <summary>
/// The rules every transport follows, asserted on the plan itself rather than on any one rendering
/// of it. The golden test pins what the browser script says; these pin what BOTH transports must
/// agree on, so the MCP side is held to the same rules without a JavaScript engine in the loop.
/// </summary>
public class WebMcpToolPlanTests
{
    private static WebMcpToolPlan Plan(string name)
        => WebMcpToolPlan.Create(
            WebMcpGoldenCatalogue.Tools().Single(tool => tool.Name == name),
            NullLogger.Instance);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ThePresetFieldIsNeverAskedOfTheAgent()
    {
        Plan("create_item").Inputs.Select(input => input.Name)
            .ShouldBe(["Name", "Enabled", "OwnerId"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void APropertyNoSchemaTypeDescribesIsSkippedNotGuessedAt()
    {
        Plan("create_item").Inputs.ShouldNotContain(input => input.Name == "Tags");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void PathParametersAreTheRequiredInputsInTheirPropertysOwnCasing()
    {
        Plan("get_item").PathProperties.ShouldBe(["Name"]);
        Plan("list_items").PathProperties.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ABodylessToolSendsItsNonPathInputsAsTheQueryString()
    {
        Plan("list_items").QueryProperties.ShouldBe(["ReferenceId", "Take", "Since"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void APathParameterIsNeverAlsoSentAsQuery()
    {
        Plan("delete_item").QueryProperties.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ABodyCarryingToolSendsNoQueryString()
    {
        var plan = Plan("create_item");
        plan.HasBody.ShouldBeTrue();
        plan.QueryProperties.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TheRouteIsKeptAsPartsSoEachValueIsEncodedOnTheWayIn()
    {
        Plan("get_item").RouteParts.ShouldBe(
        [
            new WebMcpRoutePart("/items/", IsParameter: false),
            new WebMcpRoutePart("Name", IsParameter: true),
        ]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void RecoveryIsOfferedOnlyWithAParentListAndExactlyOneParameter()
    {
        Plan("get_item").RecoveryProperty.ShouldBe("Name");
        Plan("list_items").RecoveryProperty.ShouldBeNull();
        Plan("create_item").RecoveryProperty.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AParameterThatBindsToNothingIsRefusedRatherThanGuessed()
    {
        // The registry refuses such a tool before it is ever served. Reaching the plan means a
        // descriptor was built by hand wrongly, and guessing a binding would hand the agent a URL
        // that cannot be built.
        var unbound = new WebMcpToolDescriptor("broken", "d", "/items/{Missing}", "GET", true,
            typeof(WebMcpGoldenCatalogue.ItemNameRequest), null);

        Should.Throw<InvalidOperationException>(() => WebMcpToolPlan.Create(unbound, NullLogger.Instance))
            .Message.ShouldContain("Missing");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void APresetWithNoValueIsRefusedRatherThanSentEmpty()
    {
        var presetOnly = new WebMcpToolDescriptor("broken", "d", "/items", "POST", false,
            typeof(WebMcpGoldenCatalogue.CreateItemRequest), null, PresetFieldName: "ServiceType");

        Should.Throw<InvalidOperationException>(() => WebMcpToolPlan.Create(presetOnly, NullLogger.Instance))
            .Message.ShouldContain("ServiceType");
    }
}
