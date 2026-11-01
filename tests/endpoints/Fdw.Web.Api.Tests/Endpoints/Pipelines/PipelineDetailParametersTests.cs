using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Endpoints;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// Tests for the declared run parameters the pipeline detail answers (FDW-1557): whatever kind of pipeline it is, the forms that start
/// or schedule a run read them in one shape.
/// </summary>
public sealed class PipelineDetailParametersTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void AGraphPipelineAnswersItsDeclaredParametersInTheShapeTheFormsRead()
    {
        var graph = new GraphPipelineConfiguration { Name = "Sample", Implementation = "Graph" };
        graph.Parameters.Add(new PipelineParameterConfiguration { Name = "Limit", ParameterType = "Int32", IsRequired = true, Description = "Most rows to read" });
        graph.Parameters.Add(new PipelineParameterConfiguration { Name = "Region", ParameterType = "String", IsReadOnly = true });
        graph.Parameters.Add(new PipelineParameterConfiguration { Name = "Cols", ParameterType = "String", IsList = true });

        var detail = PipelineDetailMapper.Map(graph);

        var parameters = detail.Parameters.ShouldNotBeNull();
        parameters.Count.ShouldBe(3);
        parameters[0].Name.ShouldBe("Limit");
        parameters[0].Type.ShouldBe("Int32");
        parameters[0].Required.ShouldBeTrue();
        parameters[0].Description.ShouldBe("Most rows to read");
        parameters[1].Name.ShouldBe("Region");
        parameters[1].Required.ShouldBeFalse();
        parameters[1].Description.ShouldBeNull();
        parameters[0].IsList.ShouldBeFalse();
        parameters[2].Name.ShouldBe("Cols");
        parameters[2].IsList.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void AGraphPipelineWithNoParametersAnswersNone()
    {
        PipelineDetailMapper.Map(new GraphPipelineConfiguration { Name = "Sample", Implementation = "Graph" }).Parameters.ShouldBeNull();
    }
}
