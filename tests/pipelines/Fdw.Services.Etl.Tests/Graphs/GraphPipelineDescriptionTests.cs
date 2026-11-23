using System;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// A graph pipeline's description and its parameters' are not columns of <c>pipe.GraphPipelineConfiguration</c> and <c>pipe.PipelineParameter</c> (FDW-7323, a group of FDW-6208):
/// the mappers of the two records name no Description, so no read selects the column and no save writes it, and the pipeline hands its parameters to the description provider
/// through <see cref="IPipelineImplementationConfiguration.DescribedParameters"/>.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class GraphPipelineDescriptionTests
{
    [Theory]
    [Trait("Priority", "P0")]
    [InlineData(nameof(GraphPipelineConfiguration))]
    [InlineData(nameof(PipelineParameterConfiguration))]
    public void TheMapperOfARecordNamesNoDescriptionSoNoReadSelectsTheColumnAndNoSaveWritesIt(string record)
    {
        var mapper = PocoMapperCollection.ByName(record);

        mapper.ShouldNotBeSameAs(PocoMapperCollection.NotFound);
        mapper.GetPropertyNames().ShouldNotContain("Description");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheGraphHandsItsOwnParameterObjectsToTheDescriptionProviderSoAFillReachesThem()
    {
        var region = new PipelineParameterConfiguration { Id = Guid.NewGuid(), Name = "Region" };
        var graph = new GraphPipelineConfiguration { Name = "Consolidate", Parameters = [region] };

        var described = graph.DescribedParameters.ShouldHaveSingleItem();
        described.ShouldBeSameAs(region);
        described.Id.ShouldBe(region.Id);
        described.Name.ShouldBe("Region");
    }
}
