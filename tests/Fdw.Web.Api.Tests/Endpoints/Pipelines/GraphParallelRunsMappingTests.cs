using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// FDW-3117: how many runs of a pipeline may run at once, and what happens to a run past that, are read from the graph a save states,
/// written back by a read, and shown in the pipeline's detail. A save that omits either is refused: neither has a default.
/// </summary>
public sealed class GraphParallelRunsMappingTests
{
    private static GraphPayload Payload() => new()
    {
        Name = "Nightly",
        IsEnabled = true,
        LinkCapacity = 4,
        MaxParallelRuns = 3,
        OverflowBehavior = "Refuse",
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheTwoValuesAreReadIntoTheStoredGraphWrittenBackAndShownInTheDetail()
    {
        var read = GraphPayloadReader.Read(Payload(), System.Guid.NewGuid());

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        read.Value!.MaxParallelRuns.ShouldBe(3);
        read.Value.OverflowBehavior.ShouldBe("Refuse");
        GraphPayloadWriter.Write(read.Value).MaxParallelRuns.ShouldBe(3);
        GraphPayloadWriter.Write(read.Value).OverflowBehavior.ShouldBe("Refuse");
        PipelineDetailMapper.Map(read.Value).MaxParallelRuns.ShouldBe(3);
        PipelineDetailMapper.Map(read.Value).OverflowBehavior.ShouldBe("Refuse");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void APayloadThatOmitsEitherValueIsRefusedNamingIt()
    {
        var noLimit = Payload();
        noLimit.MaxParallelRuns = null;
        var noBehavior = Payload();
        noBehavior.OverflowBehavior = null;
        var blankBehavior = Payload();
        blankBehavior.OverflowBehavior = " ";

        GraphPayloadReader.Read(noLimit, System.Guid.NewGuid()).CurrentMessage.ShouldNotBeNull().ShouldContain("maxParallelRuns");
        GraphPayloadReader.Read(noBehavior, System.Guid.NewGuid()).CurrentMessage.ShouldNotBeNull().ShouldContain("overflowBehavior");
        GraphPayloadReader.Read(blankBehavior, System.Guid.NewGuid()).IsFailure.ShouldBeTrue();
    }
}
