using Fdw.Services.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Abstractions.Runs;
using Fdw.Services.Pipelines.Runs;
using Fdw.Services.Pipelines.TestDouble;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Pipelines.Tests.Runs;

/// <summary>
/// Tests for <see cref="PipelineRuns"/> (FDW-6131, FDW-6244): a start reads the pipeline, takes its pipeline type from the implementation its domain row
/// names (the test double's <see cref="EchoPipelineType"/>, run mode Queued) and hands the run to the runner of that type's run mode; an implementation no
/// pipeline type names and a mode no runner carries out are refused.
/// </summary>
public sealed class PipelineRunsTests
{
    private static readonly Guid PipelineIdentity = Guid.Parse("44444444-0000-0000-0000-000000000004");
    private static readonly Guid Tenant = Guid.Parse("22222222-0000-0000-0000-000000000002");
    private static readonly Guid Execution = Guid.Parse("33333333-0000-0000-0000-000000000003");

    private readonly Mock<IPipelineConfigurationProvider> _pipelines = new();
    private readonly Mock<IExecutionRunControl> _runControl = new();
    private readonly Mock<IExecutionTracker> _tracker = new();
    private readonly Mock<IPipelineRunner> _queued = new();

    public PipelineRunsTests()
    {
        _queued.SetupGet(r => r.RunMode).Returns(PipelineRunModes.ByName("Queued"));
        _queued
            .Setup(r => r.Start(It.IsAny<PipelineStart>(), It.IsAny<IPipelineImplementationConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid>.Success(Execution));
    }

    private PipelineRuns Sut(params IPipelineRunner[] runners) =>
        new(_pipelines.Object, runners, _runControl.Object, _tracker.Object, Mock.Of<IAuthenticationContextAccessor>(), NullLogger<PipelineRuns>.Instance);

    private IPipelineImplementationConfiguration Pipeline(string implementation)
    {
        var pipeline = new Mock<IPipelineImplementationConfiguration>();
        pipeline.SetupGet(p => p.Id).Returns(PipelineIdentity);
        pipeline.SetupGet(p => p.Name).Returns("Consolidate");
        pipeline.SetupGet(p => p.Implementation).Returns(implementation);
        _pipelines.Setup(p => p.Get(It.IsAny<string>(),ConfigurationLevels.WholeAggregate, It.Is<ConfigurationReadOptions>(o => !o.FillsMembers), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPipelineImplementationConfiguration>.Success(pipeline.Object));
        _pipelines.Setup(p => p.Get(It.IsAny<Guid>(),ConfigurationLevels.WholeAggregate, It.Is<ConfigurationReadOptions>(o => !o.FillsMembers), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPipelineImplementationConfiguration>.Success(pipeline.Object));
        return pipeline.Object;
    }

    private static PipelineStart Start(Guid? id = null, string? name = "Consolidate", bool awaitCompletion = false) => new()
    {
        PipelineId = id,
        PipelineName = id is null ? name : null,
        TriggerSource = "Pipeline:Orders",
        TenantId = Tenant,
        RunRoles = ["etl-runner"],
        Parameters = [],
        AwaitCompletion = awaitCompletion
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task APipelineIsHandedToTheRunnerOfItsPipelineTypesRunMode()
    {
        var pipeline = Pipeline(EchoPipelineType.OptionName);
        var start = Start();

        var result = await Sut(_queued.Object).Start(start, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldBe(Execution);
        _queued.Verify(r => r.Start(start, pipeline, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AnImplementationNoPipelineTypeNamesIsRefusedAsPipelineTypeUnknown()
    {
        Pipeline("Telepathic");

        var result = await Sut(_queued.Object).Start(Start(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("PIPELINERUNS-60830");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Telepathic");
        _queued.Verify(
            r => r.Start(It.IsAny<PipelineStart>(), It.IsAny<IPipelineImplementationConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task APipelineTypeWhoseRunModeHasNoRunnerInThisHostIsRefusedAsPipelineRunModeUnsupported()
    {
        Pipeline(EchoPipelineType.OptionName);
        var atOnce = new Mock<IPipelineRunner>();
        atOnce.SetupGet(r => r.RunMode).Returns(PipelineRunModes.ByName("AtOnce"));

        var result = await Sut(atOnce.Object).Start(Start(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("PIPELINERUNS-60831");
        atOnce.Verify(
            r => r.Start(It.IsAny<PipelineStart>(), It.IsAny<IPipelineImplementationConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(true, true, "both")]
    [InlineData(false, false, "neither")]
    public async Task AStartThatNamesThePipelineByBothOrNeitherIsRefusedBeforeAnythingIsRead(bool byId, bool byName, string named)
    {
        Pipeline(EchoPipelineType.OptionName);
        var start = new PipelineStart
        {
            PipelineId = byId ? PipelineIdentity : null,
            PipelineName = byName ? "Consolidate" : null,
            TriggerSource = "Pipeline:Orders",
            TenantId = Tenant,
            RunRoles = ["etl-runner"],
            Parameters = [],
            AwaitCompletion = false
        };

        var result = await Sut(_queued.Object).Start(start, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain(named);
        _pipelines.Verify(p => p.Get(It.IsAny<Guid>(),ConfigurationLevels.WholeAggregate, It.Is<ConfigurationReadOptions>(o => !o.FillsMembers), It.IsAny<CancellationToken>()), Times.Never);
        _pipelines.Verify(p => p.Get(It.IsAny<string>(),ConfigurationLevels.WholeAggregate, It.Is<ConfigurationReadOptions>(o => !o.FillsMembers), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task APipelineTheCallerCannotReadIsRefusedNamingIt()
    {
        _pipelines.Setup(p => p.Get(It.IsAny<string>(),ConfigurationLevels.WholeAggregate, It.Is<ConfigurationReadOptions>(o => !o.FillsMembers), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPipelineImplementationConfiguration>.Failure(new GenericMessage("not found")));

        var result = await Sut(_queued.Object).Start(Start(name: "Gone"), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Gone");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ARunStartedWithItsCompletionKeptIsAwaitedThroughTheRunnerThatStartedIt()
    {
        Pipeline(EchoPipelineType.OptionName);
        _queued.Setup(r => r.Await(Execution, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<bool>.Success(true));
        var runs = Sut(_queued.Object);

        await runs.Start(Start(awaitCompletion: true), TestContext.Current.CancellationToken);
        var awaited = await runs.Await(Execution, TestContext.Current.CancellationToken);

        awaited.IsSuccess.ShouldBeTrue(awaited.CurrentMessage);
        awaited.Value.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ARunNotStartedHereToBeAwaitedIsRefused()
    {
        var awaited = await Sut(_queued.Object).Await(Execution, TestContext.Current.CancellationToken);

        awaited.IsFailure.ShouldBeTrue();
        awaited.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("PIPELINERUNS-20831");
        _queued.Verify(r => r.Await(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void StopReachesTheRunThroughTheRunControl()
    {
        _runControl.Setup(c => c.Cancel(Execution)).Returns(GenericResult.Success());

        var stopped = Sut(_queued.Object).Stop(Execution, "a person cancelled it");

        stopped.IsSuccess.ShouldBeTrue();
        _runControl.Verify(c => c.Cancel(Execution), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task FollowNamesTheRunsLiveGroupAndItsState()
    {
        var item = new Mock<IExecutionItem>();
        item.SetupGet(i => i.Id).Returns(Execution);
        item.SetupGet(i => i.State).Returns(ExecutionStateTypes.Running);
        _tracker.Setup(t => t.GetItem(Execution, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IExecutionItem>.Success(item.Object));

        var followed = await Sut(_queued.Object).Follow(Execution, TestContext.Current.CancellationToken);

        followed.IsSuccess.ShouldBeTrue(followed.CurrentMessage);
        followed.Value.ShouldNotBeNull().LiveGroup.ShouldBe("execution:33333333-0000-0000-0000-000000000003");
        followed.Value.State.ShouldBe(ExecutionStateTypes.Running.Name);
    }
}
