using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// FDW-1937: whether a pipeline requires approval to run is read from the graph a save states and written back by a read, and a payload
/// that omits it states false.
/// </summary>
public sealed class GraphRequireApprovalMappingTests
{
    private static GraphPayload Payload(bool requireApprovalToRun) => new()
    {
        Name = "Nightly",
        IsEnabled = true,
        LinkCapacity = 4,
        RequireApprovalToRun = requireApprovalToRun,
    };

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(true)]
    [InlineData(false)]
    public void TheSettingIsReadIntoTheStoredGraphAndWrittenBack(bool requireApprovalToRun)
    {
        var read = GraphPayloadReader.Read(Payload(requireApprovalToRun), System.Guid.NewGuid());

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        read.Value!.RequireApprovalToRun.ShouldBe(requireApprovalToRun);
        GraphPayloadWriter.Write(read.Value).RequireApprovalToRun.ShouldBe(requireApprovalToRun);
        PipelineDetailMapper.Map(read.Value).RequireApprovalToRun.ShouldBe(requireApprovalToRun);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void APayloadThatOmitsTheSettingStatesFalse()
    {
        var payload = new GraphPayload { Name = "Nightly", IsEnabled = true, LinkCapacity = 4 };

        payload.RequireApprovalToRun.ShouldBeFalse();
        GraphPayloadReader.Read(payload, System.Guid.NewGuid()).Value!.RequireApprovalToRun.ShouldBeFalse();
    }
}
