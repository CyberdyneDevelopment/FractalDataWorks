using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Endpoints;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// A pipeline's description is a column of the engine's own row, so every engine must carry it and the
/// detail read must answer it. It is not a column of the shared <c>pipe.Pipeline</c> domain row: that row is
/// written once (two tables cite its Id with a foreign key), and a listed field would re-version it on every
/// change. These tests pin the engine side, because a missing half drops the description silently (FDW-837).
/// </summary>
public sealed class PipelineDescriptionTests
{
    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(nameof(GraphPipelineConfiguration))]
    public void EveryEngineCarriesADescription(string engine)
    {
        // Arrange / Act
        var properties = PocoMapperCollection.ByName(engine).GetPropertyNames();

        // Assert
        properties.ShouldContain("Description");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheDetailReadAnswersWithTheDescriptionTheReadStamped()
    {
        // Arrange
        var pipeline = new GraphPipelineConfiguration { Name = "Load", Implementation = "Graph", Description = "Loads the daily feed" };

        // Act
        var detail = PipelineDetailMapper.Map(pipeline);

        // Assert
        detail.Description.ShouldBe("Loads the daily feed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TheDetailReadLeavesTheDescriptionEmptyWhenNobodyWroteOne()
    {
        // Arrange
        var pipeline = new GraphPipelineConfiguration { Name = "Load", Implementation = "Graph" };

        // Act
        var detail = PipelineDetailMapper.Map(pipeline);

        // Assert
        detail.Description.ShouldBeNull();
    }
}
