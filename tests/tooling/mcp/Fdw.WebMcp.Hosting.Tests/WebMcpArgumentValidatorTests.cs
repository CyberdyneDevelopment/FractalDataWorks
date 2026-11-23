using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.WebMcp.Hosting.Tests;

/// <summary>
/// A call is checked against the description the tool gave, before the endpoint is reached (FDW-6143): type, required
/// members, list items and object members, each refusal naming the member's path.
/// </summary>
public class WebMcpArgumentValidatorTests
{
    private static WebMcpToolPlan Plan(WebMcpToolDescriptor tool)
    {
        var created = WebMcpToolPlan.Create(tool, WebMcpCompositeCatalog.MaxDepth, NullLogger.Instance);
        created.IsSuccess.ShouldBeTrue(created.CurrentMessage);
        return created.Value.ShouldNotBeNull();
    }

    private static Fdw.Results.IGenericResult Check(WebMcpToolDescriptor tool, string json)
        => WebMcpArgumentValidator.Validate(
            Plan(tool),
            JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json),
            NullLogger.Instance);

    private static string Refusal(WebMcpToolDescriptor tool, string json)
    {
        var result = Check(tool, json);
        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldNotBeNull().ShouldEndWith("-20002");
        return result.CurrentMessage.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ACallThatMatchesTheDescriptionPasses()
    {
        var result = Check(
            WebMcpCompositeCatalog.Meeting,
            """{"Title":"Sync","Recipients":["a@x.io","b@x.io"],"Attendees":[{"Email":"a@x.io","Seats":2}],"Recurrence":{"Pattern":"weekly","Count":4}}""");

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AWrongListItemIsRefusedNamingItsIndex()
    {
        Refusal(WebMcpCompositeCatalog.Meeting, """{"Recipients":["a@x.io",7],"Attendees":[]}""")
            .ShouldContain("Recipients[1]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AMissingRequiredMemberOfAListItemIsRefusedNamingItsPath()
    {
        Refusal(WebMcpCompositeCatalog.Meeting, """{"Attendees":[{"Email":"a@x.io","Seats":1},{"Seats":3}]}""")
            .ShouldContain("Attendees[1].Email");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ANullRequiredMemberIsRefusedAsMissing()
    {
        Refusal(WebMcpCompositeCatalog.Meeting, """{"Attendees":[{"Email":null,"Seats":1}]}""")
            .ShouldContain("Attendees[0].Email");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AMissingRequiredMemberOfTheRequestIsRefused()
    {
        Refusal(WebMcpCompositeCatalog.Meeting, """{"Title":"Sync"}""").ShouldContain("Attendees");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AnObjectGivenWhereAListIsDescribedIsRefused()
    {
        Refusal(WebMcpCompositeCatalog.Meeting, """{"Attendees":{"Email":"a@x.io","Seats":1}}""")
            .ShouldContain("Attendees");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AWrongTypeInANestedObjectIsRefusedNamingItsPathAndTheTypes()
    {
        var message = Refusal(WebMcpCompositeCatalog.Meeting, """{"Attendees":[],"Recurrence":{"Count":"four"}}""");

        message.ShouldContain("Recurrence.Count");
        message.ShouldContain("integer");
        message.ShouldContain("string");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EveryProblemIsNamedInOneRefusal()
    {
        var message = Refusal(WebMcpCompositeCatalog.Meeting, """{"Recipients":[1],"Attendees":[{"Seats":1}]}""");

        message.ShouldContain("Recipients[0]");
        message.ShouldContain("Attendees[0].Email");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ACallWithNoArgumentsIsCheckedAsAnEmptySet()
    {
        var result = WebMcpArgumentValidator.Validate(Plan(WebMcpCompositeCatalog.Meeting), arguments: null, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Attendees");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AnArgumentTheDescriptionDoesNotNameIsLeftToTheEndpoint()
    {
        // The schema states no additionalProperties: false, so a member it does not name is not a mismatch with it.
        Check(WebMcpCompositeCatalog.Meeting, """{"Attendees":[],"Unlisted":true}""").IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AScalarThatTravelsInTheJsonBodyIsCheckedForItsType()
    {
        var create = WebMcpGoldenCatalog.Tools()[2];

        Check(create, """{"Name":"a","Enabled":true}""").IsSuccess.ShouldBeTrue();
        Refusal(create, """{"Name":"a","Enabled":"yes"}""").ShouldContain("Enabled");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AScalarThatTravelsAsTextInTheQueryStringIsLeftToTheEndpointsBinder()
    {
        // "5" for Take binds in the query string today; refusing it here would turn a call that works into a refusal.
        Check(WebMcpGoldenCatalog.Tools()[0], """{"Take":"5"}""").IsSuccess.ShouldBeTrue();
    }
}
