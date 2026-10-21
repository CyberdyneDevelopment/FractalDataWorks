using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.Services.Pipelines.Webhooks;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Tests.Webhooks;

/// <summary>Reading the values of a call to a webhook trigger out of its JSON body (FDW-1884).</summary>
public sealed class WebhookBodyMapperTests
{
    private static WebhookTriggerParameterMapConfiguration Map(string path, string parameter) => new() { BodyPath = path, ParameterName = parameter };

    private static List<KeyValuePair<string, string>> Values(string body, params WebhookTriggerParameterMapConfiguration[] maps)
    {
        var read = WebhookBodyMapper.Map(body, maps);
        read.IsSuccess.ShouldBeTrue();
        return [.. read.Value!];
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AValueAtAPathBecomesTheParameterItIsMappedTo()
    {
        var values = Values("""{ "region": "EU", "order": { "limit": 50, "all": true } }""",
            Map("region", "Region"), Map("order.limit", "Limit"), Map("order.all", "All"));

        values.Select(pair => $"{pair.Key}={pair.Value}").ShouldBe(["Region=EU", "Limit=50", "All=true"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AnArrayIsGivenAsItsJsonTextSoAListParameterCanTakeIt()
    {
        var values = Values("""{ "cols": ["Name", "City"] }""", Map("cols", "Columns"));

        values.ShouldHaveSingleItem().Value.ShouldBe("""["Name", "City"]""");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    [InlineData("""{ "other": 1 }""")]
    [InlineData("""{ "region": null }""")]
    [InlineData("""{ "region": { } }""")]
    [InlineData("""[1, 2]""")]
    [InlineData("")]
    public void APathThatFindsNothingSuppliesNothing(string body)
    {
        var path = body.Contains("{ }", System.StringComparison.Ordinal) ? "region.deeper" : "region";

        Values(body, Map(path, "Region")).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ABodyThatIsNotJsonIsRefusedWhenAMappingNeedsIt()
    {
        var read = WebhookBodyMapper.Map("{ nope", [Map("region", "Region")]);

        read.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ABodyIsNotReadWhenThereAreNoMappings()
    {
        var read = WebhookBodyMapper.Map("{ nope", []);

        read.IsSuccess.ShouldBeTrue();
        read.Value.ShouldBeEmpty();
    }
}
