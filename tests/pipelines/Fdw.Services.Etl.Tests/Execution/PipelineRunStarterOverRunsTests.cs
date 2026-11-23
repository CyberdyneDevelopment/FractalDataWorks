using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Abstractions.Runs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for <see cref="PipelineRunStarter"/> built over the pipeline layer's run contract (FDW-6131): a run a run starts is handed to
/// <see cref="IPipelineRuns"/> as a start under the starter's execution, and awaited through it.
/// </summary>
public sealed class PipelineRunStarterOverRunsTests
{
    private static readonly Guid Parent = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid Tenant = Guid.Parse("22222222-0000-0000-0000-000000000002");
    private static readonly Guid Child = Guid.Parse("33333333-0000-0000-0000-000000000003");

    private readonly Mock<IPipelineRuns> _runs = new();
    private readonly Mock<IPipelineConfigurationProvider> _pipelines = new();

    private PipelineRunStarter Sut() => new(_runs.Object, _pipelines.Object, NullLogger<PipelineRunStarter>.Instance);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AStartIsHandedToTheRunContractUnderTheStartersExecution()
    {
        PipelineStart? handed = null;
        _runs.Setup(r => r.Start(It.IsAny<PipelineStart>(), It.IsAny<CancellationToken>()))
            .Callback<PipelineStart, CancellationToken>((start, _) => handed = start)
            .ReturnsAsync(GenericResult<Guid>.Success(Child));

        var result = await Sut().Start(
            new PipelineRunStart
            {
                ParentExecutionId = Parent,
                PipelineName = "Consolidate",
                TenantId = Tenant,
                RunRoles = ["etl-runner"],
                TriggerSource = "Pipeline:Orders",
                Parameters = [new KeyValuePair<string, string>("Region", "ERCOT")],
                AwaitCompletion = true
            },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldBe(Child);
        handed.ShouldNotBeNull();
        handed.ParentExecutionId.ShouldBe(Parent);
        handed.PipelineName.ShouldBe("Consolidate");
        handed.PipelineId.ShouldBeNull();
        handed.TenantId.ShouldBe(Tenant);
        handed.TriggerSource.ShouldBe("Pipeline:Orders");
        handed.AwaitCompletion.ShouldBeTrue();
        handed.Opened.ShouldBeNull();
        handed.Parameters.ShouldHaveSingleItem().Value.ShouldBe("ERCOT");
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task AwaitAnswersWhatTheRunContractAnswersAndARefusedWaitIsNotASuccess(bool awaitable, bool expected)
    {
        _runs.Setup(r => r.Await(Child, It.IsAny<CancellationToken>())).ReturnsAsync(awaitable
            ? GenericResult<bool>.Success(true)
            : GenericResult<bool>.Failure(new GenericMessage("not started here")));

        (await Sut().Await(Child, TestContext.Current.CancellationToken)).ShouldBe(expected);
    }
}
