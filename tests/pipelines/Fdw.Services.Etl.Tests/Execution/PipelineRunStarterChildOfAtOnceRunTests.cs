using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Abstractions.Runs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for the start <see cref="PipelineRunStarter"/> makes under another execution (FDW-6701, slice 2d of FDW-6141): a run whose parent is an at-once run held by
/// <see cref="PipelineAtOnceRuns"/> is started at once, under what is left of its parent's limits. A parent the host does not hold as an at-once run changes nothing.
/// </summary>
public sealed class PipelineRunStarterChildOfAtOnceRunTests : IDisposable
{
    private static readonly Guid Parent = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid Tenant = Guid.Parse("22222222-0000-0000-0000-000000000002");
    private static readonly Guid Child = Guid.Parse("33333333-0000-0000-0000-000000000003");

    private readonly Mock<IPipelineRuns> _runs = new();
    private readonly Mock<IPipelineConfigurationProvider> _pipelines = new();
    private readonly PipelineAtOnceRuns _atOnce = new();
    private PipelineStart? _handed;

    public PipelineRunStarterChildOfAtOnceRunTests()
    {
        _runs.Setup(r => r.Start(It.IsAny<PipelineStart>(), It.IsAny<CancellationToken>()))
            .Callback<PipelineStart, CancellationToken>((start, _) => _handed = start)
            .ReturnsAsync(GenericResult<Guid>.Success(Child));
    }

    public void Dispose() => _atOnce.Dispose();

    private PipelineRunStarter Sut() => new(_runs.Object, _pipelines.Object, _atOnce, NullLogger<PipelineRunStarter>.Instance);

    private static PipelineRunStart Start(Guid parent) => new()
    {
        ParentExecutionId = parent,
        PipelineName = "Report",
        TenantId = Tenant,
        RunRoles = ["etl-runner"],
        TriggerSource = "Pipeline:Chat",
        Parameters = new List<KeyValuePair<string, string>>(),
        AwaitCompletion = false,
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AStartUnderAnAtOnceRunRequiresAtOnceAndCarriesTheParentsLimits()
    {
        var limits = PipelineRunTimeAndStepLimits.Start(DateTimeOffset.UtcNow, maxRunSeconds: 60, maxSteps: 10);
        using var held = _atOnce.HoldLimits(Parent, limits);

        var result = await Sut().Start(Start(Parent), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _handed.ShouldNotBeNull();
        _handed.RequiredRunMode.ShouldBe("AtOnce");
        _handed.ParentLimits.ShouldBeSameAs(limits);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AStartUnderARunTheHostDoesNotHoldAsAtOnceRequiresNoRunModeAndCarriesNoLimits()
    {
        var result = await Sut().Start(Start(Parent), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _handed.ShouldNotBeNull();
        _handed.RequiredRunMode.ShouldBeNull();
        _handed.ParentLimits.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AParentWhoseRunHasEndedIsNoLongerHeld()
    {
        var limits = PipelineRunTimeAndStepLimits.Start(DateTimeOffset.UtcNow, maxRunSeconds: 60, maxSteps: 10);
        _atOnce.HoldLimits(Parent, limits).Dispose();

        var result = await Sut().Start(Start(Parent), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _handed.ShouldNotBeNull();
        _handed.RequiredRunMode.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AStartUnderAParentTheHostHoldsNoLimitsOfRequiresNoRunModeAndCarriesNoLimits()
    {
        // Why a parent id that nothing holds and not no parent: the start of a run states its parent (it is required), so "no limits to count against" is a parent the host holds no at-once run for.
        var result = await Sut().Start(Start(Child), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _handed.ShouldNotBeNull();
        _handed.RequiredRunMode.ShouldBeNull();
        _handed.ParentLimits.ShouldBeNull();
    }
}
