using System.Collections.Generic;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-1689: a source or destination may name what it reads or writes as a run parameter. The run renders the value in before the
/// component is built, the stored component is not changed, a parameter the run has no value for fails the run naming it, the graph check
/// refuses a name the pipeline does not declare, and the description leaves a source named by a parameter with unknown fields.
/// </summary>
public sealed class ComponentParametersTests
{
    private static PipelineComponentConfiguration FileSource(string container) => new()
    {
        Name = "file", ComponentType = "ContainerSource", DataStoreName = "Landing", PathName = "inbound", ContainerName = container, BatchSize = 50,
    };

    private static FlowBuildContext ContextWith(IReadOnlyDictionary<string, object?> parameters) =>
        GraphTestKit.ContextWith(new GraphPipelineConfiguration { Name = "Ingest" }, parameters);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AParameterInTheContainerIsReplacedByTheValueTheRunWasGiven()
    {
        var stored = FileSource("{{File}}");

        var rendered = ComponentParameters.Render(stored, ContextWith(new Dictionary<string, object?> { ["file"] = "orders.csv" }));

        rendered.IsSuccess.ShouldBeTrue(rendered.CurrentMessage);
        rendered.Value!.ContainerName.ShouldBe("orders.csv");
        rendered.Value.DataStoreName.ShouldBe("Landing");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ARunStartedAsATestRunRendersTheParameterAsARealRunDoes()
    {
        var context = GraphTestKit.ContextWith(
            new GraphPipelineConfiguration { Name = "Ingest" }, new Dictionary<string, object?> { ["BlobName"] = "orders.csv" }, new GraphTestRun(5, true));

        var rendered = ComponentParameters.Render(FileSource("{{BlobName}}"), context);

        rendered.IsSuccess.ShouldBeTrue(rendered.CurrentMessage);
        rendered.Value!.ContainerName.ShouldBe("orders.csv");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheStoredComponentIsNotChangedByARun()
    {
        var stored = FileSource("{{File}}");

        ComponentParameters.Render(stored, ContextWith(new Dictionary<string, object?> { ["File"] = "orders.csv" }));

        stored.ContainerName.ShouldBe("{{File}}");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AComponentThatHoldsNoParameterIsBuiltFromTheStoredComponentItself()
    {
        var stored = FileSource("orders.csv");

        ComponentParameters.Render(stored, ContextWith(new Dictionary<string, object?>())).Value.ShouldBeSameAs(stored);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AParameterTheRunWasGivenNoValueForFailsTheRunNamingIt()
    {
        var rendered = ComponentParameters.Render(FileSource("{{File}}"), ContextWith(new Dictionary<string, object?> { ["File"] = null }));

        rendered.IsFailure.ShouldBeTrue();
        rendered.CurrentMessage.ShouldNotBeNull().ShouldContain("File");
        rendered.CurrentMessage.ShouldContain("file");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void OnlyASourceOrDestinationSettingTakesAParameter()
    {
        var filter = new PipelineComponentConfiguration { Name = "f", ComponentType = "Filter", DataSetName = "{{X}}" };

        ParameterSettings.Of(filter).ShouldBeEmpty();
        ParameterSettings.Of(FileSource("{{File}}")).ShouldContain(("container", "{{File}}"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheGraphCheckRefusesAParameterThePipelineDoesNotDeclare()
    {
        var graph = new GraphPipelineConfiguration
        {
            Name = "Ingest",
            Parameters = [new PipelineParameterConfiguration { Name = "File", ParameterType = "String" }],
            Components = [FileSource("{{File}}"), FileSource("{{Other}}")],
        };

        var problems = GraphComponentValidator.Problems(graph, [], Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);

        problems.ShouldContain(problem => problem.Reason.Contains("'Other'") && problem.Path.Contains("container"));
        problems.ShouldNotContain(problem => problem.Reason.Contains("'File'"));
    }
}
