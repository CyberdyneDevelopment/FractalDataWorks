using System;
using System.Linq;
using Fdw.Services.Etl.Graphs;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-1975: the save check judges a parameter's default. It is the designer's text of the parameter's own type; a required parameter is always
/// given, so it has none, and a list has none yet. Each refusal names the parameter and the default.
/// </summary>
public sealed class ParameterDefaultSaveCheckTests
{
    private static GraphPipelineConfiguration With(PipelineParameterConfiguration parameter)
    {
        var graph = Graph([DataSetSource("src"), Mapped("map", ("Key", "Id"))], [Link("src", "map")], Dropped("map"));
        graph.Parameters.Add(parameter);
        return graph;
    }

    private static string[] DefaultProblems(PipelineParameterConfiguration parameter) =>
        [.. GraphShapeValidator.Problems(With(parameter), NullLogger.Instance)
            .Where(problem => problem.Path.SequenceEqual(["parameters", parameter.Name, "defaultValue"]))
            .Select(problem => problem.Reason)];

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AnOptionalParameterWithAReadableDefaultIsAccepted()
    {
        DefaultProblems(new PipelineParameterConfiguration { Name = "Limit", ParameterType = "Int64", DefaultText = "25" }).ShouldBeEmpty();
        DefaultProblems(new PipelineParameterConfiguration { Name = "File", ParameterType = "String", DefaultText = "orders.csv" }).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ARequiredParameterThatDeclaresADefaultIsRefused()
    {
        var reason = DefaultProblems(new PipelineParameterConfiguration { Name = "File", ParameterType = "String", IsRequired = true, DefaultText = "orders.csv" }).ShouldHaveSingleItem();

        reason.ShouldContain("File");
        reason.ShouldContain("required");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AListParameterThatDeclaresADefaultIsRefused()
    {
        DefaultProblems(new PipelineParameterConfiguration { Name = "Files", ParameterType = "String", IsList = true, DefaultText = "[\"a\"]" })
            .ShouldHaveSingleItem().ShouldContain("list");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ADefaultThatIsNotAValueOfTheParametersTypeIsRefused()
    {
        var reason = DefaultProblems(new PipelineParameterConfiguration { Name = "Limit", ParameterType = "Int64", DefaultText = "many" }).ShouldHaveSingleItem();

        reason.ShouldContain("Limit");
        reason.ShouldContain("Int64");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AParameterWithNoDefaultHasNothingToRefuse()
    {
        DefaultProblems(new PipelineParameterConfiguration { Name = "File", ParameterType = "String" }).ShouldBeEmpty();
    }
}
