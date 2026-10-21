using System.Linq;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Endpoints;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// Tests for what the describe answer lists in scope (FDW-2006): the graph's parameters and variables, and the run facts the engine declares in every
/// graph, read-only and not parameters, taken from <see cref="RunFacts"/> so a future fact is listed with no other change.
/// </summary>
public sealed class GraphScopeEntriesTests
{
    private static GraphPipelineConfiguration Graph() => new()
    {
        Name = "Windowed",
        Parameters = [new PipelineParameterConfiguration { Name = "File", ParameterType = "String", IsRequired = true }],
        Variables = [new PipelineVariableConfiguration { Name = "Count", DataType = "Int32" }],
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheRunFactsAreListedAsReadOnlyDatesThatAreNotParameters()
    {
        var entries = GraphScopeEntries.Of(Graph());

        foreach (var fact in RunFacts.Names)
        {
            var entry = entries.Single(candidate => candidate.Name == fact);
            entry.DataType.ShouldBe("DateTimeOffset");
            entry.IsReadOnly.ShouldBeTrue();
            entry.IsParameter.ShouldBeFalse();
            entry.IsRequired.ShouldBeFalse();
        }

        entries.Select(entry => entry.Name).ShouldContain("RunStarted");
        entries.Select(entry => entry.Name).ShouldContain("LastSuccessfulRun");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheGraphsOwnParametersAndVariablesAreStillListedBeforeTheFacts()
    {
        var entries = GraphScopeEntries.Of(Graph());

        entries.Select(entry => entry.Name).ShouldBe(["File", "Count", .. RunFacts.Names]);
        entries[0].IsParameter.ShouldBeTrue();
        entries[0].IsRequired.ShouldBeTrue();
        entries[1].IsParameter.ShouldBeFalse();
    }
}
