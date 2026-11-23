using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.WebMcp.Hosting.Tests;

/// <summary>
/// A tool describes its whole request: lists, objects and nested members, not only scalars (FDW-6143).
/// </summary>
public class WebMcpToolPlanCompositeTests
{
    private static WebMcpToolPlan Plan(WebMcpToolDescriptor tool, int maxDepth = WebMcpCompositeCatalog.MaxDepth)
    {
        var created = WebMcpToolPlan.Create(tool, maxDepth, NullLogger.Instance);
        created.IsSuccess.ShouldBeTrue(created.CurrentMessage);
        return created.Value.ShouldNotBeNull();
    }

    private static WebMcpToolInput Input(WebMcpToolPlan plan, string name)
        => plan.Inputs.Single(input => input.Name == name);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AListOfScalarsIsAnArrayWhoseItemsAreDescribed()
    {
        var recipients = Input(Plan(WebMcpCompositeCatalog.Meeting), "Recipients");

        recipients.JsonType.ShouldBe("array");
        recipients.Items.ShouldNotBeNull().JsonType.ShouldBe("string");
        recipients.Properties.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AListOfObjectsIsAnArrayOfObjectsWithTheirMembers()
    {
        var attendees = Input(Plan(WebMcpCompositeCatalog.Meeting), "Attendees");

        var item = attendees.Items.ShouldNotBeNull();
        item.JsonType.ShouldBe("object");
        item.Properties.ShouldNotBeNull().Select(member => member.Name).ShouldBe(["Email", "Seats", "Note"]);
        item.Properties.Single(member => member.Name == "Email").JsonType.ShouldBe("string");
        item.Properties.Single(member => member.Name == "Seats").JsonType.ShouldBe("integer");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AnObjectMemberIsAnObjectWithItsMembers()
    {
        var recurrence = Input(Plan(WebMcpCompositeCatalog.Meeting), "Recurrence");

        recurrence.JsonType.ShouldBe("object");
        recurrence.Properties.ShouldNotBeNull().Select(member => member.Name).ShouldBe(["Pattern", "Count"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ARequiredModifierOrANonNullableValueMakesANestedMemberRequired()
    {
        var item = Input(Plan(WebMcpCompositeCatalog.Meeting), "Attendees").Items.ShouldNotBeNull();

        item.Properties.ShouldNotBeNull();
        item.Properties.Single(member => member.Name == "Email").Required.ShouldBeTrue();
        item.Properties.Single(member => member.Name == "Seats").Required.ShouldBeTrue();
        item.Properties.Single(member => member.Name == "Note").Required.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ARequiredCompositeMemberOfTheRequestIsRequiredBesideThePathParameters()
    {
        var plan = Plan(WebMcpCompositeCatalog.Meeting);

        plan.RequiredProperties.ShouldBe(["Attendees"]);
        plan.PathProperties.ShouldBeEmpty();
        Plan(WebMcpCompositeCatalog.MeetingByRoom).RequiredProperties.ShouldBe(["Room"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AMemberNoSchemaShapeDescribesIsSkippedNotGuessedAt()
    {
        var plan = Plan(WebMcpCompositeCatalog.Meeting);

        plan.Inputs.ShouldNotContain(input => input.Name == "Labels");
        plan.Inputs.ShouldNotContain(input => input.Name == "Blob");
        plan.Inputs.Select(input => input.Name).ShouldBe(["Title", "Recipients", "Attendees", "Recurrence"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ANestingExactlyAtTheStatedDepthIsDescribed()
    {
        Plan(WebMcpCompositeCatalog.Meeting, maxDepth: 2).Inputs.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ANestingBeyondTheStatedDepthIsRefusedNamingTheToolAndTheSetting()
    {
        var created = WebMcpToolPlan.Create(WebMcpCompositeCatalog.Meeting, maxDepth: 1, NullLogger.Instance);

        created.IsFailure.ShouldBeTrue();
        created.Messages[^1].Code.ShouldNotBeNull().ShouldEndWith("-61015");
        created.CurrentMessage.ShouldNotBeNull().ShouldContain("create_meeting");
        created.CurrentMessage.ShouldContain("WebMcpToolSchemaMaxDepth");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AChainDeeperThanTheStatedDepthIsRefusedAtTheMemberThatCrossesIt()
    {
        var created = WebMcpToolPlan.Create(WebMcpCompositeCatalog.Deep, WebMcpCompositeCatalog.MaxDepth, NullLogger.Instance);

        created.IsFailure.ShouldBeTrue();
        created.Messages[^1].Code.ShouldNotBeNull().ShouldEndWith("-61015");
        created.CurrentMessage.ShouldNotBeNull().ShouldContain("First.Items[].Inner");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ATypeThatReachesItselfIsRefusedNamingThePath()
    {
        var created = WebMcpToolPlan.Create(WebMcpCompositeCatalog.Cyclic, WebMcpCompositeCatalog.MaxDepth, NullLogger.Instance);

        created.IsFailure.ShouldBeTrue();
        created.Messages[^1].Code.ShouldNotBeNull().ShouldEndWith("-61016");
        created.CurrentMessage.ShouldNotBeNull().ShouldContain("Next");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ATypeThatReachesItselfThroughAListIsRefusedToo()
    {
        var created = WebMcpToolPlan.Create(WebMcpCompositeCatalog.CyclicThroughAList, WebMcpCompositeCatalog.MaxDepth, NullLogger.Instance);

        created.IsFailure.ShouldBeTrue();
        created.Messages[^1].Code.ShouldNotBeNull().ShouldEndWith("-61016");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AScalarOnlyToolDescribesItselfAsBefore()
    {
        var plan = Plan(WebMcpGoldenCatalog.Tools().Single(tool => tool.Name == "list_items"));

        plan.Inputs.ShouldAllBe(input => input.Items == null && input.Properties == null && !input.Required);
        plan.RequiredProperties.ShouldBe(plan.PathProperties);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ADepthOfNothingIsRefusedRatherThanReadAsNoLimit()
    {
        Should.Throw<System.ArgumentOutOfRangeException>(
            () => WebMcpToolPlan.Create(WebMcpCompositeCatalog.Meeting, maxDepth: 0, NullLogger.Instance));
    }
}
