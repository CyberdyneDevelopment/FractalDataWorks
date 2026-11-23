using Fdw.Data;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Endpoints;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// A pipeline's description is a member of the engine's record, so every engine must carry it and the detail read
/// must answer it. Since 1.6.0 (FDW-7323) it is not a column of any pipeline table: the member is <c>[NotStored]</c>,
/// the mapper names no <c>Description</c>, and the pipeline provider writes and fills the text through the
/// <c>Pipeline</c> description type. These tests pin the engine side, because a missing half drops the description
/// silently (FDW-837).
/// </summary>
public sealed class PipelineDescriptionTests
{
    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(typeof(GraphPipelineConfiguration))]
    public void EveryEngineCarriesADescriptionMemberThatIsNoColumnAndIsFilledFromThePipelineDescriptionType(Type engine)
    {
        // Arrange / Act
        var member = engine.GetProperty("Description");
        var properties = PocoMapperCollection.ByName(engine.Name).GetPropertyNames();

        // Assert
        member.ShouldNotBeNull();
        member.PropertyType.ShouldBe(typeof(string));
        member.GetCustomAttributes(typeof(NotStoredAttribute), inherit: false).ShouldNotBeEmpty();
        properties.ShouldNotContain("Description");
        DescriptionTypes.ByName(PipelineDescriptionType.OptionName).ShouldNotBeSameAs(DescriptionTypes.NotFound);
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
