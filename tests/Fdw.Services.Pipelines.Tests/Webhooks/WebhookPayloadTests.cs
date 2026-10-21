using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.Services.Pipelines.Webhooks;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Tests.Webhooks;

/// <summary>Reading and writing a webhook trigger (FDW-1884): what is refused on save, and that the secret never leaves.</summary>
public sealed class WebhookPayloadTests
{
    private static readonly WebhookSecretHash Secret = new("a1b2c3", "Abcdefgh", new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero));

    private static GraphPipelineConfiguration Pipeline() => new()
    {
        Id = Guid.NewGuid(),
        Name = "BreweriesIngest",
        Implementation = "Graph",
        Parameters = [new PipelineParameterConfiguration { Name = "Region", ParameterType = "String" }],
    };

    private static WebhookPayload Payload() => new()
    {
        Name = "partner-feed",
        Pipeline = "BreweriesIngest",
        Enabled = true,
        Description = "Called by the partner when a file lands",
        RunRoles = ["Operator"],
        Parameters = [new WebhookParameterMapPayload { BodyPath = "order.region", Parameter = "Region" }],
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void AGoodTriggerIsReadWithItsPipelineRolesMappingsAndSecretHash()
    {
        var pipeline = Pipeline();

        var read = WebhookPayloadReader.Read(Payload(), Guid.NewGuid(), pipeline, ["Operator"], Secret);

        read.IsSuccess.ShouldBeTrue();
        var trigger = read.Value!;
        trigger.PipelineId.ShouldBe(pipeline.Id);
        trigger.PipelineName.ShouldBe("BreweriesIngest");
        trigger.IsEnabled.ShouldBeTrue();
        trigger.Domain.ShouldBe("WebhookTrigger");
        trigger.Implementation.ShouldBe("Webhook");
        trigger.RunRoleNames.ShouldBe(["Operator"]);
        trigger.ParameterMaps.ShouldHaveSingleItem().ParameterName.ShouldBe("Region");
        trigger.SecretHash.ShouldBe("a1b2c3");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ATriggerThatDoesNotStateWhetherItIsOnIsRefused()
    {
        var payload = Payload();
        payload.Enabled = null;

        WebhookChecker.Problems(payload, ["Region"]).ShouldContain("enabled is not stated");
        WebhookPayloadReader.Read(payload, Guid.NewGuid(), Pipeline(), ["Operator"], Secret).IsFailure.ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    [InlineData("")]
    [InlineData("has space")]
    [InlineData("-leading")]
    [InlineData("slash/inside")]
    public void ANameThatCannotBePartOfAnAddressIsRefused(string name)
    {
        var payload = Payload();
        payload.Name = name;

        WebhookChecker.Problems(payload, ["Region"]).ShouldContain(problem => problem.StartsWith("the name must be", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void AMappingToAParameterThePipelineDoesNotDeclareIsRefusedNamingIt()
    {
        var payload = Payload();
        payload.Parameters.Add(new WebhookParameterMapPayload { BodyPath = "x", Parameter = "Nope" });

        WebhookChecker.Problems(payload, ["Region"]).ShouldContain("the pipeline 'BreweriesIngest' does not declare the parameter 'Nope'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void TheSameParameterMappedTwiceIsRefused()
    {
        var payload = Payload();
        payload.Parameters.Add(new WebhookParameterMapPayload { BodyPath = "other", Parameter = "region" });

        WebhookChecker.Problems(payload, ["Region"]).ShouldContain("the parameter 'region' is mapped more than once");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ATriggerWithNoRunRoleIsRefused()
    {
        var payload = Payload();
        payload.RunRoles = [];

        WebhookChecker.Problems(payload, ["Region"]).ShouldContain("no run role is named");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TheAnswerNeverCarriesTheSecretHashAndTheTypeHasNoMemberForIt()
    {
        var read = WebhookPayloadReader.Read(Payload(), Guid.NewGuid(), Pipeline(), ["Operator"], Secret);

        var written = WebhookPayloadWriter.Write(read.Value!);
        var json = JsonSerializer.Serialize(written);

        json.ShouldNotContain("a1b2c3");
        json.ShouldNotContain("Hash", Case.Insensitive);
        written.SecretPrefix.ShouldBe("Abcdefgh");
        typeof(WebhookPayload).GetProperties().Select(property => property.Name).ShouldNotContain(name => name.Contains("Hash", StringComparison.OrdinalIgnoreCase));
        typeof(WebhookSummaryPayload).GetProperties().Select(property => property.Name).ShouldNotContain(name => name.Contains("Hash", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AWrittenTriggerGivesBackWhatWasRead()
    {
        var read = WebhookPayloadReader.Read(Payload(), Guid.NewGuid(), Pipeline(), ["Operator"], Secret);

        var written = WebhookPayloadWriter.Write(read.Value!);

        written.Name.ShouldBe("partner-feed");
        written.Pipeline.ShouldBe("BreweriesIngest");
        written.Enabled.ShouldBe(true);
        written.RunRoles.ShouldBe(["Operator"]);
        written.Parameters.ShouldHaveSingleItem().BodyPath.ShouldBe("order.region");
    }
}
