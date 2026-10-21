using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Transformations.Configuration;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// Tests for <see cref="StageChainLookup"/> (FDW-1136, FDW-2000): the Transformations that a Map component's mapping chain names are looked up
/// before the pipeline is saved, and every problem with a chain is placed on its component, so create, update and both validate
/// endpoints refuse the same things.
/// </summary>
public sealed class StageChainLookupTests
{
    private static readonly Guid TrimId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static StageChainLookup Lookup(params ITransformationImplementationConfiguration[] known)
    {
        var provider = new Mock<ITransformationConfigurationProvider>();
        provider
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, CancellationToken _) => Found(known.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))));
        provider
            .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => Found(known.FirstOrDefault(item => item.Id == id)));
        return new StageChainLookup(provider.Object, NullLogger.Instance);
    }

    private static IGenericResult<ITransformationImplementationConfiguration> Found(ITransformationImplementationConfiguration? found) =>
        found is null
            ? GenericResult<ITransformationImplementationConfiguration>.Failure(new GenericMessage("not found"))
            : GenericResult<ITransformationImplementationConfiguration>.Success(found);

    private static FieldTransformationConfiguration Trim() => new()
    {
        Id = TrimId,
        Name = "Trim",
        Implementation = FieldTransformationConfiguration.ImplementationName,
        TransformationType = "Trim",
    };

    private static List<MapChain> Maps(params PipelineFieldTransformationReference[] chain) =>
        chain.Length == 0 ? [] : [new MapChain("Shape", 0, "A", [.. chain])];

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ANamedStepIsResolvedToTheIdOfItsTransformation()
    {
        // Act
        var chains = await Lookup(Trim()).Resolve("Nightly", null, Maps(new PipelineFieldTransformationReference { Transformation = "trim", Ordinal = 1 }), TestContext.Current.CancellationToken);

        // Assert
        chains.Problems.ShouldBeEmpty();
        chains.Ids["Trim"].ShouldBe(TrimId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStepThatGivesTheIdOfAnExistingTransformationHasNoProblem()
    {
        // Act
        var chains = await Lookup(Trim()).Resolve("Nightly", null, Maps(new PipelineFieldTransformationReference { TransformationId = TrimId, Ordinal = 1 }), TestContext.Current.CancellationToken);

        // Assert
        chains.Problems.ShouldBeEmpty();
        chains.Refusal<int>(NullLogger.Instance).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARequestThatStatesNoChainHasNothingToLookUp()
    {
        // Act
        var chains = await Lookup().Resolve("Nightly", null, Maps(), TestContext.Current.CancellationToken);

        // Assert
        chains.Problems.ShouldBeEmpty();
        chains.Ids.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStepNamingATransformationThatDoesNotExistIsAProblemOnItsStage()
    {
        // Act
        var chains = await Lookup(Trim()).Resolve("Nightly", null, Maps(new PipelineFieldTransformationReference { Transformation = "Missing", Ordinal = 1 }), TestContext.Current.CancellationToken);

        // Assert
        var problem = chains.Problems.ShouldHaveSingleItem();
        problem.Stage.ShouldBe("Shape");
        problem.Field.ShouldBe("Maps[0].Transformations[0].Transformation");
        problem.Reason.ShouldContain("Missing");
        chains.Refusal<int>(NullLogger.Instance)!.Code!.Id.ShouldBe(20001);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStepGivingAnIdThatDoesNotExistIsAProblem()
    {
        // Act
        var chains = await Lookup().Resolve("Nightly", null, Maps(new PipelineFieldTransformationReference { TransformationId = TrimId, Ordinal = 1 }), TestContext.Current.CancellationToken);

        // Assert
        chains.Problems.ShouldHaveSingleItem().Reason.ShouldContain(TrimId.ToString());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStepWhoseTransformationDoesNotWorkOnOneFieldIsAProblem()
    {
        // Arrange
        var wholeInput = new Mock<ITransformationImplementationConfiguration>();
        wholeInput.SetupGet(item => item.Id).Returns(TrimId);
        wholeInput.SetupGet(item => item.Name).Returns("Totals");
        wholeInput.SetupGet(item => item.Implementation).Returns("Aggregate");

        // Act
        var chains = await Lookup(wholeInput.Object).Resolve("Nightly", null, Maps(new PipelineFieldTransformationReference { Transformation = "Totals", Ordinal = 1 }), TestContext.Current.CancellationToken);

        // Assert
        chains.Problems.ShouldHaveSingleItem().Reason.ShouldContain("Aggregate");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("both")]
    [InlineData("neither")]
    [InlineData("gap")]
    public async Task AChainThatIsNotWellFormedIsAProblemAndNothingIsLookedUpForIt(string defect)
    {
        // Arrange
        var step = defect switch
        {
            "both" => new PipelineFieldTransformationReference { Transformation = "Trim", TransformationId = TrimId, Ordinal = 1 },
            "neither" => new PipelineFieldTransformationReference { Ordinal = 1 },
            _ => new PipelineFieldTransformationReference { Transformation = "Trim", Ordinal = 2 },
        };

        // Act
        var chains = await Lookup(Trim()).Resolve("Nightly", null, Maps(step), TestContext.Current.CancellationToken);

        // Assert
        chains.Problems.ShouldNotBeEmpty();
        chains.Problems.ShouldAllBe(problem => problem.Stage == "Shape");
    }
}
