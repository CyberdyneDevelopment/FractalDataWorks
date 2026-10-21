using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-1942: the data set, data store, path and container of a source or destination, and the values of a SQL source's script
/// parameters, may name a variable the pipeline declares, as they may name a run parameter. The variable must be one no step changes,
/// because these settings are read when the run starts; the run renders it with its starting value.
/// </summary>
public sealed class PipelineVariableSettingsTests
{
    private static PipelineVariableConfiguration Variable(string name, string type, string value) => new() { Name = name, DataType = type, ValueText = value };

    private static GraphPipelineConfiguration WithSource(string dataSet, params PipelineVariableConfiguration[] variables)
    {
        var graph = Steps(DataFlow("flow", [DataSetSource("src", dataSet)], [], []));
        foreach (var variable in variables)
        {
            graph.Variables.Add(variable);
        }

        return graph;
    }

    private static string[] Reasons(GraphPipelineConfiguration graph, string name) =>
        [.. GraphComponentValidator.Problems(graph, [], NullLogger.Instance).Select(problem => problem.Reason).Where(reason => reason.Contains(name))];

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AVariableNoStepChangesMayBeNamedInASourceSetting()
    {
        Reasons(WithSource("{{Folder}}", Variable("Folder", "String", "incoming")), "Folder").ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AVariableAStepSetsIsRefusedInASourceSettingNamingTheVariable()
    {
        var graph = WithSource("{{Folder}}", Variable("Folder", "String", "incoming"));
        graph.Components.Add(SetVariable("set", "Folder", "String", "later", ordinal: 1));

        var reason = Reasons(graph, "Folder").ShouldHaveSingleItem();

        reason.ShouldContain("a step sets 'Folder'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AnUndeclaredNameIsRefusedAndTheReasonSaysParameterOrVariable()
    {
        Reasons(WithSource("{{Missing}}"), "Missing").ShouldHaveSingleItem().ShouldContain("declares no parameter or variable 'Missing'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheValueOfAScriptParameterMayNameAVariableButNotAnUndeclaredName()
    {
        var script = new PipelineComponentConfiguration { Name = "query", ComponentType = "SqlSource" };
        script.Fields.Add(new PipelineComponentFieldConfiguration { SettingName = "parameters", TargetField = "Limit", ConstantType = "Int32", ConstantText = "{{Rows}}" });
        script.Fields.Add(new PipelineComponentFieldConfiguration { SettingName = "parameters", Ordinal = 1, TargetField = "Day", ConstantType = "String", ConstantText = "{{Gone}}" });
        var graph = Steps(DataFlow("flow", [script], [], []));
        graph.Variables.Add(Variable("Rows", "Int32", "5"));

        Reasons(graph, "Rows").ShouldBeEmpty();
        Reasons(graph, "Gone").ShouldHaveSingleItem().ShouldContain("declares no parameter or variable 'Gone'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheRunRendersTheStartingValueOfEachVariableNoStepChangesAndLeavesOutTheOnesAStepSets()
    {
        var graph = WithSource("{{Folder}}", Variable("Folder", "String", "incoming"), Variable("Count", "Int32", "7"), Variable("Moves", "Int32", "0"));
        graph.Components.Add(SetVariable("set", "Moves", "Int32", "1", ordinal: 1));

        var values = GraphScope.RenderValues(graph, new Dictionary<string, object?> { ["Which"] = "Orders" }, NullLogger.Instance);

        values.IsSuccess.ShouldBeTrue(values.CurrentMessage);
        values.Value!["Folder"].ShouldBe("incoming");
        values.Value["Count"].ShouldBe(7);
        values.Value["Which"].ShouldBe("Orders");
        values.Value.ContainsKey("Moves").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AStartingValueThatIsNotOfTheVariablesTypeFailsNamingTheVariable()
    {
        var graph = WithSource("{{Count}}", Variable("Count", "Int32", "seven"));

        var values = GraphScope.RenderValues(graph, new Dictionary<string, object?>(), NullLogger.Instance);

        values.IsFailure.ShouldBeTrue();
        values.CurrentMessage.ShouldNotBeNull().ShouldContain("Count");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void WhatTheRunReachesIsJudgedForTheNameAVariableGives()
    {
        var graph = WithSource("{{Folder}}", Variable("Folder", "String", "Archive"));

        var uses = GraphPipelineUses.Of(graph, new Dictionary<string, object?>(), NullLogger.Instance);

        uses.IsSuccess.ShouldBeTrue(uses.CurrentMessage);
        uses.Value!.ShouldContain(use => use.Name == "Archive");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AScriptParameterNamingAParameterOrAVariableIsReadAsTheTypeTheRowStates()
    {
        var script = new PipelineComponentConfiguration { Name = "query", ComponentType = "SqlSource" };
        script.Fields.Add(new PipelineComponentFieldConfiguration { SettingName = "parameters", TargetField = "Limit", ConstantType = "Int32", ConstantText = "{{Rows}}" });
        script.Fields.Add(new PipelineComponentFieldConfiguration { SettingName = "parameters", Ordinal = 1, TargetField = "Region", ConstantType = "String", ConstantText = "north" });
        var graph = Steps(DataFlow("flow", [script], [], []));

        var read = ScriptParameters.Of(script, ScriptParameters.Setting, ContextWith(graph, new Dictionary<string, object?> { ["Rows"] = 5 }));

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        read.Value!.Select(parameter => (parameter.Name, parameter.Value)).ShouldBe([("Limit", (object?)5), ("Region", "north")]);
        ScriptParameters.Of(script, ScriptParameters.Setting, ContextWith(graph, new Dictionary<string, object?>())).IsFailure.ShouldBeTrue();
    }
}
