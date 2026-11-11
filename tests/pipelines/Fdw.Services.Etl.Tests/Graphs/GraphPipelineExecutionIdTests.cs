using System;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// A graph run is the execution that tracks it (FDW-4956): a run whose options carry no execution id is refused with FLOW-20970, never given
/// a minted id no execution record names.
/// </summary>
public sealed class GraphPipelineExecutionIdTests
{
    private static GraphPipeline Pipeline() =>
        new(new GraphPipelineConfiguration { Name = "Orders" }, NullLogger<GraphPipeline>.Instance, Mock.Of<IFlowExecutor>()) { CommittedReportSeconds = 10, UnmatchedKeySample = 10 };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunWhoseOptionsCarryNoExecutionIdIsRefused()
    {
        var ran = await Pipeline().Execute(new PipelineExecutionOptions { TenantId = Guid.NewGuid() }, TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("no execution id");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunStartedWithoutOptionsIsRefusedTheSameWay()
    {
        var ran = await Pipeline().Execute(TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("no execution id");
    }
}
