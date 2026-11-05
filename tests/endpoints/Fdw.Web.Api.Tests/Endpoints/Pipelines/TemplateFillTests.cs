using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// Tests for making a new pipeline from a template (FDW-3379): the parameters that are given are filled in wherever their {{Name}} stands and are no longer
/// parameters, the others stay run parameters, and what a run would refuse is refused naming the parameter.
/// </summary>
public sealed class TemplateFillTests
{
    private static readonly System.DateTimeOffset Version = new(2026, 11, 4, 12, 0, 0, System.TimeSpan.Zero);

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    private static GraphPayload Template()
    {
        var source = new GraphComponentPayload { Name = "src", Kind = "SqlSource" };
        source.Settings["script"] = Json("\"SELECT Id FROM {{SourceTable}} WHERE Year = @Year -- {{Region}}\"");
        source.Settings["maxRows"] = Json("100");

        var split = new GraphComponentPayload { Name = "split", Kind = "ConditionalSplit" };
        split.Settings["outputs"] = Json("[{\"name\":\"{{Tier}}\",\"filter\":\"Amount > 100\"}]");

        return new GraphPayload
        {
            Name = "TeamsTemplate",
            Description = "Loads {{SourceTable}}",
            IsEnabled = true,
            LinkCapacity = 4,
            Parameters =
            [
                new GraphParameterPayload { Name = "SourceTable", DataType = "String", IsRequired = true, DefaultValue = "dbo.Team", Description = "The table to read" },
                new GraphParameterPayload { Name = "Tier", DataType = "String", DefaultValue = "gold" },
                new GraphParameterPayload { Name = "Region", DataType = "String" },
                new GraphParameterPayload { Name = "Year", DataType = "Int32" },
            ],
            Components = [source, split],
        };
    }

    // The text of one setting of one component, which a test asserts is there.
    private static string Setting(GraphPayload payload, string component, string setting) =>
        payload.Components.Single(c => c.Name == component).Settings[setting].ShouldNotBeNull().ToString().ShouldNotBeNull();

    private static List<KeyValuePair<string, string>> Values(params (string Name, string Text)[] values) =>
        [.. values.Select(pair => new KeyValuePair<string, string>(pair.Name, pair.Text))];

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AGivenParameterIsFilledInEverywhereItsReferenceStandsAndIsNoLongerAParameter()
    {
        var result = TemplateFill.Apply(Template(), Version, "Orders", Values(("SourceTable", "dbo.Orders"), ("Tier", "silver")), NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var made = result.Value!;
        made.Name.ShouldBe("Orders");
        made.Description.ShouldBe("Loads dbo.Orders");
        Setting(made, "src", "script").ShouldBe("SELECT Id FROM dbo.Orders WHERE Year = @Year -- {{Region}}");
        Setting(made, "split", "outputs").ShouldContain("\"name\":\"silver\"");
        made.Parameters.Select(p => p.Name).ShouldBe(["Region", "Year"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AParameterThatIsNotGivenStaysAParameterAndItsReferenceStaysInTheText()
    {
        var result = TemplateFill.Apply(Template(), Version, "Orders", Values(("SourceTable", "dbo.Orders")), NullLogger.Instance);

        var made = result.Value!;
        made.Parameters.Select(p => p.Name).ShouldBe(["Tier", "Region", "Year"]);
        Setting(made, "src", "script").ShouldContain("{{Region}}");
        Setting(made, "split", "outputs").ShouldContain("{{Tier}}");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheTemplateItselfIsLeftAsItWas()
    {
        var template = Template();

        TemplateFill.Apply(template, Version, "Orders", Values(("SourceTable", "dbo.Orders")), NullLogger.Instance);

        template.Name.ShouldBe("TeamsTemplate");
        template.Parameters.Count.ShouldBe(4);
        Setting(template, "src", "script").ShouldContain("{{SourceTable}}");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AValueIsReadAsTheTypeTheParameterDeclares()
    {
        var typed = TemplateFill.Apply(Template(), Version, "Orders", Values(("Year", "2026")), NullLogger.Instance);
        var wrong = TemplateFill.Apply(Template(), Version, "Orders", Values(("Year", "last year")), NullLogger.Instance);

        typed.IsSuccess.ShouldBeTrue(typed.CurrentMessage);
        wrong.IsFailure.ShouldBeTrue();
        wrong.CurrentMessage.ShouldNotBeNull().ShouldContain("Year");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ANameTheTemplateDoesNotDeclareAndANameGivenTwiceAreRefused()
    {
        TemplateFill.Apply(Template(), Version, "Orders", Values(("Nowhere", "x")), NullLogger.Instance).IsFailure.ShouldBeTrue();
        TemplateFill.Apply(Template(), Version, "Orders", Values(("Tier", "a"), ("Tier", "b")), NullLogger.Instance).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ATextValueThatCouldChangeWhatAScriptDoesIsRefused()
    {
        var result = TemplateFill.Apply(Template(), Version, "Orders", Values(("SourceTable", "dbo.Team; DROP TABLE x")), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("SourceTable");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheNewPipelineIsNotATemplateAndRecordsTheTemplateAndItsVersion()
    {
        var template = Template();
        template.IsTemplate = true;

        var made = TemplateFill.Apply(template, Version, "Orders", Values(("SourceTable", "dbo.Orders")), NullLogger.Instance).Value!;

        made.IsTemplate.ShouldBeFalse();
        made.TemplateName.ShouldBe("TeamsTemplate");
        made.TemplateVersion.ShouldBe(Version);
        template.IsTemplate.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ATemplateWithNothingGivenComesBackAsACopyWithTheNewName()
    {
        var result = TemplateFill.Apply(Template(), Version, "Copy", [], NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Name.ShouldBe("Copy");
        result.Value.Parameters.Count.ShouldBe(4);
        result.Value.Components.Select(c => c.Name).ShouldBe(["src", "split"]);
    }
}
