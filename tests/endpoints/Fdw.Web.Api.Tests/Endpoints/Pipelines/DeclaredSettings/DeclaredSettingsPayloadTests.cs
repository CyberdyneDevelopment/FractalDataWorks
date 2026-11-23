using System.Linq;
using System.Text.Json;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines.DeclaredSettings;

/// <summary>
/// FDW-6133: a setting a component type declares that no column of the component holds is read from the graph the API states into a declared setting
/// row as invariant text, written back as it was stated, and a run parameter binding, which only a column setting takes, is refused; a setting read when
/// the step runs names variables in its text as {{Name}}.
/// </summary>
public sealed class DeclaredSettingsPayloadTests
{
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    private static GraphPayload Graph(GraphComponentPayload component) => new()
    {
        Implementation = "Graph",
        Name = "AskFlow",
        IsEnabled = true,
        LinkCapacity = 4,
        MaxParallelRuns = 1,
        OverflowBehavior = "Queue",
        Components = [component],
    };

    private static GraphComponentPayload Ask()
    {
        var ask = new GraphComponentPayload { Name = "ask", Kind = "AskModel" };
        ask.Settings["model"] = Json("\"large\"");
        ask.Settings["temperature"] = Json("0.7");
        ask.Settings["question"] = Json("\"Ask {{Question}}\"");
        return ask;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADeclaredSettingIsReadIntoADeclaredSettingRowWithNoColumn()
    {
        var read = GraphPayloadReader.Read(Graph(Ask()), System.Guid.NewGuid());

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        var component = read.Value.ShouldNotBeNull().Components.ShouldHaveSingleItem();
        component.DeclaredSettings.Single(row => row.SettingName == "model").ValueText.ShouldBe("large");
        component.DeclaredSettings.Single(row => row.SettingName == "temperature").ValueText.ShouldBe("0.7");
        component.DeclaredSettings.Single(row => row.SettingName == "question").ValueText.ShouldBe("Ask {{Question}}");
        component.SettingBindings.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void WhatASaveReadsIsWhatAReadWrites()
    {
        var graph = GraphPayloadReader.Read(Graph(Ask()), System.Guid.NewGuid()).Value.ShouldNotBeNull();

        var written = GraphPayloadWriter.Write(graph).Components.ShouldHaveSingleItem();

        written.Settings["model"].ShouldBe("large");
        written.Settings["temperature"].ShouldBe("0.7");
        written.Settings["question"].ShouldBe("Ask {{Question}}");
        var again = GraphPayloadReader.Read(new GraphPayload
        {
            Implementation = "Graph",
            Name = "AskFlow", IsEnabled = true, LinkCapacity = 4, MaxParallelRuns = 1, OverflowBehavior = "Queue", Components = [written],
        }, System.Guid.NewGuid());
        again.IsSuccess.ShouldBeTrue(again.CurrentMessage);
        again.Value.ShouldNotBeNull().Components.ShouldHaveSingleItem().DeclaredSettings.Count.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADeclaredSettingBoundToARunParameterIsRefusedNamingTheTextForm()
    {
        var ask = Ask();
        ask.Settings["model"] = Json("{\"parameter\":\"Model\"}");

        var read = GraphPayloadReader.Read(Graph(ask), System.Guid.NewGuid());

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("{{Name}}");
    }
}
