using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// A graph that leaves out values a save requires is refused once, naming every one it leaves out, as POST /pipelines/graph/validate lists
/// every problem (FDW-6521). It named only the first.
/// </summary>
public sealed class GraphMissingValuesTests
{
    private static readonly string[] Required = ["name", "isEnabled", "linkCapacity", "maxParallelRuns", "overflowBehavior"];

    private static GraphPayload Complete() => new()
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
    public void AGraphThatStatesNoneOfTheRequiredValuesIsRefusedNamingEveryOne()
    {
        var read = GraphPayloadReader.Read(new GraphPayload(), System.Guid.NewGuid());

        read.IsFailure.ShouldBeTrue();
        var message = read.CurrentMessage.ShouldNotBeNull();
        foreach (var name in Required)
            message.ShouldContain(name);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AGraphThatLeavesOutTwoValuesNamesBothAndOnlyThose()
    {
        var graph = Complete();
        graph.LinkCapacity = null;
        graph.OverflowBehavior = " ";

        var message = GraphPayloadReader.Read(graph, System.Guid.NewGuid()).CurrentMessage.ShouldNotBeNull();

        message.ShouldContain("linkCapacity");
        message.ShouldContain("overflowBehavior");
        message.ShouldNotContain("isEnabled");
        message.ShouldNotContain("maxParallelRuns");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AGraphThatStatesEveryRequiredValueIsRead()
    {
        GraphPayloadReader.Read(Complete(), System.Guid.NewGuid()).IsSuccess.ShouldBeTrue();
    }
}
