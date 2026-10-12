using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Transformations.Configuration;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Etl.Pipelines;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Pipelines.Endpoints;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// Tests for how a saved chain reads back (FDW-1136): each step carries the id it is stored by and, when the caller can read
/// that Transformation, its name, read with one query for the whole pipeline; a Transformation the caller cannot read has a
/// null name, and a failure of the read is a failure, not a missing name.
/// </summary>
public sealed class PipelineChainReadBackTests
{
    private static readonly Guid TrimId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UpperId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid HiddenId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static PipelineTransformConfiguration MapStage(string name, params (Guid TransformationId, int Ordinal)[] chain) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
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

    private static BatchCopyPipelineConfiguration Saved(params PipelineTransformConfiguration[] stages) =>
        new() { Name = "Load", Implementation = "BatchCopy", Transforms = [.. stages] };

    private static PipelineTransformConfiguration BranchOver(PipelineTransformConfiguration inner) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Route",
        OperationType = "Branch",
        Unmatched = "Pass",
        Paths = [new PipelineTransformPathConfiguration { Name = "All", Condition = "Age >= 0", Ordinal = 1, Stages = [inner] }],
    };

    private static FieldTransformationConfiguration Field(Guid id, string name) => new()
    {
        Id = id,
        Name = name,
        Implementation = FieldTransformationConfiguration.ImplementationName,
        TransformationType = name,
    };

    private static Mock<ITransformationConfigurationProvider> Readable(params ITransformationImplementationConfiguration[] readable)
    {
        var provider = new Mock<ITransformationConfigurationProvider>();
        provider
            .Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ITransformationImplementationConfiguration>>.Success(readable));
        return provider;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AStepCarriesItsIdItsNameAndItsOrdinalInOrder()
    {
        // Arrange: stored out of order
        var pipeline = Saved(MapStage("Shape", (UpperId, 2), (TrimId, 1)));
        var names = new Dictionary<Guid, string> { [TrimId] = "Trim", [UpperId] = "ToUpper" };

        // Act
        var chain = PipelineDetailMapper.Map(pipeline, names).Transforms![0].FieldMappings![0].Transforms!;

        // Assert
        chain.Select(step => (step.TransformationId, step.Transformation, step.Ordinal))
            .ShouldBe(new (Guid?, string?, int)[] { (TrimId, "Trim", 1), (UpperId, "ToUpper", 2) });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AStepWhoseTransformationTheCallerCannotReadHasANullNameAndKeepsItsId()
    {
        // Arrange
        var pipeline = Saved(MapStage("Shape", (TrimId, 1), (HiddenId, 2)));
        var names = new Dictionary<Guid, string> { [TrimId] = "Trim" };

        // Act
        var chain = PipelineDetailMapper.Map(pipeline, names).Transforms![0].FieldMappings![0].Transforms!;

        // Assert
        chain[1].TransformationId.ShouldBe(HiddenId);
        chain[1].Transformation.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AMappingWithNoChainReadsBackNullNotAnEmptyList()
    {
        // Act
        var mapping = PipelineDetailMapper.Map(Saved(MapStage("Shape")), new Dictionary<Guid, string>()).Transforms![0].FieldMappings![0];

        // Assert
        mapping.Transforms.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task DetailReadsEveryNameWithOneQueryAndHidesNoneThatTheCallerCanRead()
    {
        // Arrange: three steps over two mappings, one Transformation not readable by the caller
        var pipeline = Saved(MapStage("Shape", (TrimId, 1), (UpperId, 2)), MapStage("Second", (HiddenId, 1)));
        var provider = Readable(Field(TrimId, "Trim"), Field(UpperId, "ToUpper"));

        // Act
        var detail = await PipelineDetailMapper.Detail(pipeline, provider.Object, TestContext.Current.CancellationToken);

        // Assert
        detail.IsSuccess.ShouldBeTrue(detail.CurrentMessage);
        provider.Verify(p => p.Get(It.IsAny<CancellationToken>()), Times.Once);
        detail.Value!.Transforms![0].FieldMappings![0].Transforms!.Select(step => step.Transformation).ShouldBe(new string?[] { "Trim", "ToUpper" });
        detail.Value.Transforms![1].FieldMappings![0].Transforms![0].Transformation.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task APipelineWithNoChainAsksTheProviderNothing()
    {
        // Arrange
        var provider = Readable();

        // Act
        var detail = await PipelineDetailMapper.Detail(Saved(MapStage("Shape")), provider.Object, TestContext.Current.CancellationToken);

        // Assert
        detail.IsSuccess.ShouldBeTrue(detail.CurrentMessage);
        provider.Verify(p => p.Get(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AChainOnAMapStageInsideABranchPathReadsBackItsNames()
    {
        // Arrange
        var pipeline = Saved(BranchOver(MapStage("Shape", (TrimId, 1))));
        var provider = Readable(Field(TrimId, "Trim"));

        // Act
        var detail = await PipelineDetailMapper.Detail(pipeline, provider.Object, TestContext.Current.CancellationToken);

        // Assert
        var nested = detail.Value!.Transforms![0].Paths![0].Stages[0];
        nested.FieldMappings![0].Transforms!.Single().Transformation.ShouldBe("Trim");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFailureToReadTheTransformationsIsReturnedNotShownAsMissingNames()
    {
        // Arrange
        var provider = new Mock<ITransformationConfigurationProvider>();
        provider
            .Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ITransformationImplementationConfiguration>>.Failure(new GenericMessage("store unavailable")));

        // Act
        var detail = await PipelineDetailMapper.Detail(Saved(MapStage("Shape", (TrimId, 1))), provider.Object, TestContext.Current.CancellationToken);

        // Assert
        detail.IsSuccess.ShouldBeFalse();
    }
}
