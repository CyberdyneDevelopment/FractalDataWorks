using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Transformations.Configuration;
using Fdw.Data.Transformations;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// Tests for <see cref="FieldTransformChains"/> (FDW-1136, FDW-2000): the chains of named Transformations that a Map component's
/// mappings state are resolved once, when the run starts, and anything that cannot be resolved fails the run, naming the component
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

    private static PipelineComponentConfiguration Stage(params (Guid TransformationId, int Ordinal)[] chain) => new()
    {
        Name = "Shape",
        ComponentType = "Filter",
        Fields =
        [
            new PipelineComponentFieldConfiguration
            {
                Id = Guid.NewGuid(),
                SettingName = "maps",
                TargetField = "A",
                SourceField = "a",
                TransformationChain =
                [
                    .. chain.Select(step => new PipelineComponentFieldTransformationConfiguration { TransformationId = step.TransformationId, Ordinal = step.Ordinal }),
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
        var chain = result.Value![stage.Fields[0].Id];
        chain.Select(step => (step.TransformType, step.Ordinal)).ShouldBe(new[] { ("Trim", 1), ("ToUpper", 2) });
        chain[1].Parameters["Culture"].ShouldBe("en-US");
    }
}
