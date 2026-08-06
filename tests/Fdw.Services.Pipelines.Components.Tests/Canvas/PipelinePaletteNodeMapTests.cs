using Fdw.Services.Pipelines.Components.Canvas;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Components.Tests.Canvas;

/// <summary>
/// Tests for <see cref="PipelinePaletteNodeMap"/>.
/// </summary>
public sealed class PipelinePaletteNodeMapTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MapSourceReturnsDataSetNodeTypeWithSourceRole()
    {
        var result = PipelinePaletteNodeMap.Map("Source", 0);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.NodeType.Name.ShouldBe("DataSet");
        result.Value!.Metadata[PipelineCanvasMetadataKeys.DataSetRole].ShouldBe(PipelineCanvasMetadataKeys.RoleSource);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MapDestinationReturnsDataSetNodeTypeWithSinkRole()
    {
        var result = PipelinePaletteNodeMap.Map("Destination", 0);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.NodeType.Name.ShouldBe("DataSet");
        result.Value!.Metadata[PipelineCanvasMetadataKeys.DataSetRole].ShouldBe(PipelineCanvasMetadataKeys.RoleSink);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    [InlineData("Map")]
    [InlineData("Filter")]
    [InlineData("Aggregate")]
    [InlineData("Calculate")]
    [InlineData("Lookup")]
    public void MapTransformNameReturnsTransformNodeTypeWithOperationTypeAndExecutionOrder(string paletteName)
    {
        var result = PipelinePaletteNodeMap.Map(paletteName, 3);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.NodeType.Name.ShouldBe("Transform");
        result.Value!.Metadata[PipelineCanvasMetadataKeys.OperationType].ShouldBe(paletteName);
        result.Value!.Metadata[PipelineCanvasMetadataKeys.ExecutionOrder].ShouldBe("3");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MapUnknownPaletteNameReturnsFailure()
    {
        var result = PipelinePaletteNodeMap.Map("NotARealPaletteEntry", 0);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }
}
