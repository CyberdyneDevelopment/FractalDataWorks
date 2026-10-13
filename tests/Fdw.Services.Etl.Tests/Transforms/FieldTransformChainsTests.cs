using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Transformations.Configuration;
using Fdw.Data.Transformations;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// Tests for <see cref="FieldTransformChains"/> (FDW-1136): the chains of named Transformations that a Map stage's field
/// mappings state are resolved once, when the run starts, and anything that cannot be resolved fails the run, naming the stage
/// and the mapping, instead of being skipped.
/// </summary>
public sealed class FieldTransformChainsTests
{
    private static readonly Guid TrimId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UpperId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static Mock<ITransformationConfigurationProvider> Provider(params ITransformationImplementationConfiguration[] known)
    {
        var provider = new Mock<ITransformationConfigurationProvider>();
        provider
            .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
                known.FirstOrDefault(item => item.Id == id) is { } found
                    ? GenericResult<ITransformationImplementationConfiguration>.Success(found)
                    : GenericResult<ITransformationImplementationConfiguration>.Failure(new GenericMessage("not found")));
        return provider;
    }

    private static FieldTransformationConfiguration Field(Guid id, string name, string operation, params (string Name, string Value)[] parameters)
    {
        // Touch a Text transformer so its assembly's operations are registered before they are looked up by name
        _ = new ToUpperFieldTransformer();
        return new FieldTransformationConfiguration
        {
            Id = id,
            Name = name,
            Implementation = FieldTransformationConfiguration.ImplementationName,
            TransformationType = operation,
            Parameters = [.. parameters.Select(parameter => new FieldTransformationParameterConfiguration { Name = parameter.Name, Value = parameter.Value })],
        };
    }

    private static PipelineTransformConfiguration Stage(params (Guid TransformationId, int Ordinal)[] chain) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Shape",
        OperationType = "Map",
        FieldMappings =
        [
            new PipelineTransformFieldMappingConfiguration
            {
                Id = Guid.NewGuid(),
                Name = "Label",
                SourceField = "a",
                DestinationField = "A",
                TransformationChain =
                [
                    .. chain.Select(step => new PipelineTransformFieldMappingTransformationConfiguration { TransformationId = step.TransformationId, Ordinal = step.Ordinal }),
                ],
            },
        ],
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task StagesThatStateNoChainResolveToNothingWithoutAProvider()
    {
        // Act
        var result = await FieldTransformChains.Resolve([Stage()], null, NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AChainWithoutAProviderFailsTheRun()
    {
        // Act
        var result = await FieldTransformChains.Resolve([Stage((TrimId, 1))], null, NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStepWhoseTransformationNoLongerExistsFailsTheRun()
    {
        // Arrange
        var provider = Provider(Field(UpperId, "Upper", "ToUpper"));

        // Act
        var result = await FieldTransformChains.Resolve([Stage((TrimId, 1))], provider.Object, NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStepWhoseTransformationIsNotFieldLevelFailsTheRun()
    {
        // Arrange
        var wholeInput = new Mock<ITransformationImplementationConfiguration>();
        wholeInput.SetupGet(item => item.Id).Returns(TrimId);
        wholeInput.SetupGet(item => item.Name).Returns("Totals");
        wholeInput.SetupGet(item => item.Implementation).Returns("Aggregate");
        var provider = Provider(wholeInput.Object);

        // Act
        var result = await FieldTransformChains.Resolve([Stage((TrimId, 1))], provider.Object, NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStepNamingAnOperationNoOptionImplementsFailsTheRun()
    {
        // Arrange
        var provider = Provider(Field(TrimId, "Odd", "NoSuchOperation"));

        // Act
        var result = await FieldTransformChains.Resolve([Stage((TrimId, 1))], provider.Object, NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AChainResolvesToItsStepsInOrderWithTheirParametersByMappingId()
    {
        // Arrange: the steps are stated out of order
        var provider = Provider(Field(TrimId, "Trim", "Trim"), Field(UpperId, "Upper", "ToUpper", ("Culture", "en-US")));
        var stage = Stage((UpperId, 2), (TrimId, 1));

        // Act
        var result = await FieldTransformChains.Resolve([stage], provider.Object, NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var chain = result.Value![stage.FieldMappings[0].Id];
        chain.Select(step => (step.TransformType, step.Ordinal)).ShouldBe(new[] { ("Trim", 1), ("ToUpper", 2) });
        chain[1].Parameters["Culture"].ShouldBe("en-US");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ARunContextCarriesExactlyTheChainsItIsGiven()
    {
        // Arrange
        var stated = new Dictionary<Guid, IReadOnlyList<IFieldMappingTransformation>>();

        // Act
        var context = new TransformContext(Guid.NewGuid(), NullLogger.Instance, new Dictionary<string, object?>(), fieldTransformChains: stated);

        // Assert
        context.FieldTransformChains.ShouldBeSameAs(stated);
    }

    private static PipelineTransformConfiguration BranchOver(PipelineTransformConfiguration inner, bool innerEnabled = true)
    {
        inner.IsEnabled = innerEnabled;
        return new PipelineTransformConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Route",
            OperationType = "Branch",
            Unmatched = "Pass",
            Paths =
            [
                new PipelineTransformPathConfiguration { Name = "All", Condition = "Points >= 0", Ordinal = 1, Stages = [inner] },
            ],
        };
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AChainOnAMapStageInsideABranchPathIsResolvedByItsMappingId()
    {
        // Arrange
        var provider = Provider(Field(TrimId, "Trim", "Trim"));
        var inner = Stage((TrimId, 1));

        // Act
        var result = await FieldTransformChains.Resolve([BranchOver(inner)], provider.Object, NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value![inner.FieldMappings[0].Id].Select(step => step.TransformType).ShouldBe(new[] { "Trim" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AChainInsideABranchPathThatCannotBeResolvedFailsTheRunNamingTheNestedStage()
    {
        // Arrange: the Transformation the nested chain names does not exist
        var provider = Provider();

        // Act
        var result = await FieldTransformChains.Resolve([BranchOver(Stage((TrimId, 1)))], provider.Object, NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Route.All.Shape");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ADisabledStageInsideABranchPathIsNotSearchedBecauseItNeverRuns()
    {
        // Arrange: the nested chain would fail to resolve, but the stage is disabled
        var provider = Provider();

        // Act
        var result = await FieldTransformChains.Resolve([BranchOver(Stage((TrimId, 1)), innerEnabled: false)], provider.Object, NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunOverABranchAppliesTheChainOfAMapStageInsideItsPath()
    {
        // Arrange: the Map stage maps Label to Shout through ToUpper
        var provider = Provider(Field(UpperId, "Upper", "ToUpper"));
        var inner = Stage((UpperId, 1));
        inner.FieldMappings[0].SourceField = "Label";
        inner.FieldMappings[0].DestinationField = "Shout";
        var branch = BranchOver(inner);
        var chains = await FieldTransformChains.Resolve([branch], provider.Object, NullLogger.Instance, TestContext.Current.CancellationToken);
        var context = new TransformContext(Guid.NewGuid(), NullLogger.Instance, new Dictionary<string, object?>(), fieldTransformChains: chains.Value!);
        var rows = new List<IDictionary<string, object?>> { new Dictionary<string, object?> { ["Label"] = "abc", ["Points"] = 1 } };

        // Act
        var result = await new BranchTransformType().TransformBatch(rows, branch, context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Single()["Shout"].ShouldBe("ABC");
    }
}
