using Fdw.Services.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Abstractions.Runs;
using Fdw.Services.Pipelines.Runs;
using Fdw.Services.Pipelines.TestDouble;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Pipelines.Tests.Runs;

/// <summary>
/// FDW-6701 (slice 2d of FDW-6141): a start made by an AtOnce run states the run mode it requires, and <see cref="PipelineRuns"/> chooses the runner from that mode instead of from
/// the started pipeline's own type. A start that requires none is carried out by the runner of its type, as before; a required mode that is unknown or that no runner in the host
/// carries out is refused, never replaced by the type's.
/// </summary>
public sealed class PipelineRunsRequiredRunModeTests
{
    private static readonly Guid PipelineIdentity = Guid.Parse("44444444-0000-0000-0000-000000000004");
    private static readonly Guid Tenant = Guid.Parse("22222222-0000-0000-0000-000000000002");
    private static readonly Guid Execution = Guid.Parse("33333333-0000-0000-0000-000000000003");

    private readonly Mock<IPipelineConfigurationProvider> _pipelines = new();
    private readonly Mock<IExecutionRunControl> _runControl = new();
    private readonly Mock<IExecutionTracker> _tracker = new();
    private readonly Mock<IPipelineRunner> _queued = new();
    private readonly Mock<IPipelineRunner> _atOnce = new();

    public PipelineRunsRequiredRunModeTests()
    {
        _queued.SetupGet(r => r.RunMode).Returns(PipelineRunModes.ByName("Queued"));
        _atOnce.SetupGet(r => r.RunMode).Returns(PipelineRunModes.ByName("AtOnce"));
        foreach (var runner in new[] { _queued, _atOnce })
        {
            runner
                .Setup(r => r.Start(It.IsAny<PipelineStart>(), It.IsAny<IPipelineImplementationConfiguration>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<Guid>.Success(Execution));
        }
    }

    private PipelineRuns Sut(params IPipelineRunner[] runners) =>
        new(_pipelines.Object, runners, _runControl.Object, _tracker.Object, Mock.Of<IAuthenticationContextAccessor>(), NullLogger<PipelineRuns>.Instance);

    // A pipeline of the test double's Echo type, whose run mode is Queued.
    private IPipelineImplementationConfiguration QueuedTypedPipeline()
    {
        var pipeline = new Mock<IPipelineImplementationConfiguration>();
        pipeline.SetupGet(p => p.Id).Returns(PipelineIdentity);
        pipeline.SetupGet(p => p.Name).Returns("Consolidate");
        pipeline.SetupGet(p => p.Implementation).Returns(EchoPipelineType.OptionName);
        _pipelines.Setup(p => p.Get(It.IsAny<string>(),ConfigurationLevels.WholeAggregate, It.Is<ConfigurationReadOptions>(o => !o.FillsMembers), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPipelineImplementationConfiguration>.Success(pipeline.Object));
        return pipeline.Object;
    }

    private static PipelineStart Start(string? requiredRunMode) => new()
    {
        PipelineName = "Consolidate",
        TriggerSource = "Pipeline:Orders",
        TenantId = Tenant,
        RunRoles = ["etl-runner"],
        Parameters = [],
        AwaitCompletion = false,
        RequiredRunMode = requiredRunMode,
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AStartThatRequiresAtOnceIsCarriedOutByTheAtOnceRunnerWhateverThePipelinesTypeSays()
    {
        var pipeline = QueuedTypedPipeline();
        var start = Start("AtOnce");

        var result = await Sut(_queued.Object, _atOnce.Object).Start(start, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _atOnce.Verify(r => r.Start(start, pipeline, It.IsAny<CancellationToken>()), Times.Once);
        _queued.Verify(
            r => r.Start(It.IsAny<PipelineStart>(), It.IsAny<IPipelineImplementationConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AStartThatRequiresNoRunModeIsCarriedOutByTheRunnerOfItsType()
    {
        var pipeline = QueuedTypedPipeline();
        var start = Start(null);

        var result = await Sut(_queued.Object, _atOnce.Object).Start(start, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _queued.Verify(r => r.Start(start, pipeline, It.IsAny<CancellationToken>()), Times.Once);
        _atOnce.Verify(
            r => r.Start(It.IsAny<PipelineStart>(), It.IsAny<IPipelineImplementationConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARequiredModeNoRunnerInTheHostCarriesOutIsRefusedAndTheTypesRunnerIsNotUsedInItsPlace()
    {
        QueuedTypedPipeline();

        var result = await Sut(_queued.Object).Start(Start("AtOnce"), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("AtOnce");
        _queued.Verify(
            r => r.Start(It.IsAny<PipelineStart>(), It.IsAny<IPipelineImplementationConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARequiredModeNoRunModeOptionDeclaresIsRefusedNamingIt()
    {
        QueuedTypedPipeline();

        var result = await Sut(_queued.Object, _atOnce.Object).Start(Start("Teleported"), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Teleported");
        _queued.Verify(
            r => r.Start(It.IsAny<PipelineStart>(), It.IsAny<IPipelineImplementationConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
        _atOnce.Verify(
            r => r.Start(It.IsAny<PipelineStart>(), It.IsAny<IPipelineImplementationConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
