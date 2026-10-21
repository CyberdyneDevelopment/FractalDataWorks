using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-2119: a mapping on a component's input may take a single value of the run for its constant, written <c>{{Name}}</c> like a script
/// parameter: a run parameter, a variable, or a run fact. The value fills the target field in every row, it is read when the component's
/// stage starts, and the save check judges the name the way it judges any text read at that time.
/// </summary>
public sealed class MappingConstantTests
{
    private static readonly string[] MappingPath = ["components", "map", "settings", "mappings"];

    private static IReadOnlyDictionary<string, object?> Values(params (string Name, object? Value)[] values) =>
        values.ToDictionary(pair => pair.Name, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

    private static PipelineComponentConfiguration WithConstant(string text)
    {
        var component = Mapped("map", ("Key", "Id"));
        component.Fields.Add(new PipelineComponentFieldConfiguration
        {
            SettingName = "mappings", InputName = "input", Ordinal = 1, TargetField = "Loaded", ConstantType = "String", ConstantText = text,
        });
        return component;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void OnlyAConstantThatNamesAValueIsReadWhenTheStageStarts()
    {
        ParameterSettings.MappingConstants(WithConstant("plain")).ShouldBeEmpty();
        ParameterSettings.MappingConstants(WithConstant("{{Stamp}}")).ShouldHaveSingleItem().TargetField.ShouldBe("Loaded");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheValueIsRenderedIntoACopyAndTheStoredMappingIsNotChanged()
    {
        var stored = WithConstant("run {{Stamp}}");

        var rendered = ComponentParameters.RenderLate(stored, Values(("Stamp", "20261020")), NullLogger.Instance);

        rendered.IsSuccess.ShouldBeTrue(rendered.CurrentMessage);
        rendered.Value!.Fields.Single(field => field.TargetField == "Loaded").ConstantText.ShouldBe("run 20261020");
        rendered.Value.Fields.Single(field => field.TargetField == "Key").SourceField.ShouldBe("Id");
        stored.Fields.Single(field => field.TargetField == "Loaded").ConstantText.ShouldBe("run {{Stamp}}");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AValueTheRunDoesNotHaveFailsNamingTheComponentAndTheValue()
    {
        var rendered = ComponentParameters.RenderLate(WithConstant("{{Stamp}}"), Values(), NullLogger.Instance);

        rendered.IsFailure.ShouldBeTrue();
        rendered.CurrentMessage.ShouldNotBeNull().ShouldContain("Stamp");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheSaveCheckRefusesAValueNobodyDeclaredAndAcceptsADeclaredParameter()
    {
        var graph = Graph([DataSetSource("src"), WithConstant("{{Stamp}}")], [Link("src", "map")], Dropped("map"));

        var undeclared = GraphShapeValidator.Problems(graph, NullLogger.Instance).Where(problem => problem.Path.SequenceEqual(MappingPath)).ToList();

        undeclared.ShouldHaveSingleItem().Reason.ShouldContain("Stamp");

        graph.Parameters.Add(new PipelineParameterConfiguration { Name = "Stamp", ParameterType = "String" });
        GraphShapeValidator.Problems(graph, NullLogger.Instance).Where(problem => problem.Path.SequenceEqual(MappingPath)).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ARunFactIsAcceptedInAMappingConstant()
    {
        var graph = Graph([DataSetSource("src"), WithConstant("{{RunStarted}}")], [Link("src", "map")], Dropped("map"));

        GraphShapeValidator.Problems(graph, NullLogger.Instance).Where(problem => problem.Path.SequenceEqual(MappingPath)).ShouldBeEmpty();
    }
}
