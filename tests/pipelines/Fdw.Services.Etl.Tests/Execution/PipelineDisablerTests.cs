using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Graphs;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// The DisablePipeline consequence (FDW-3864): the run whose end made the rule fire holds the record of why, the configuration is marked in the save that disables the pipeline,
/// a pipeline already disabled is left alone, and a firing that cannot be recorded or carries no failures disables nothing.
/// </summary>
public sealed class PipelineDisablerTests
{
    private static readonly Guid Run = new("bbbbbbbb-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 18, 0, 0, TimeSpan.Zero);

    private sealed class Configurations(GraphPipelineConfiguration? graph, bool disableSucceeds = true) : IPipelineFailureConfigurations
    {
        public GraphPipelineConfiguration? Saved { get; private set; }

        public Task<IGenericResult<GraphPipelineConfiguration?>> Read(string pipelineName, CancellationToken cancellationToken = default) =>
            Task.FromResult<IGenericResult<GraphPipelineConfiguration?>>(GenericResult<GraphPipelineConfiguration?>.Success(graph));

        public Task<IGenericResult> Disable(GraphPipelineConfiguration pipeline, CancellationToken cancellationToken = default)
        {
            pipeline.IsEnabled = false;
            Saved = pipeline;
            return Task.FromResult(disableSucceeds ? GenericResult.Success() : GenericResult.Failure(new GenericMessage("the save was refused")));
        }
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static PipelineRunOutcome Failure(int minutes) => new(Guid.NewGuid(), Now.AddMinutes(-minutes), false, true, "Validation", "FDW-20000", null);

    private static EscalationFiring Firing(Guid? runId, params PipelineRunOutcome[] failures)
    {
        var values = new Dictionary<string, object?> { [PipelineRunThresholdType.Failures] = JsonSerializer.Serialize(failures) };
        return new EscalationFiring(
            new ThresholdSubject("Pipeline", "Orders", Guid.NewGuid(), Guid.NewGuid(), runId), "Runs", 1,
            new ThresholdRule(new Mock<IFilterNode>().Object, "DisablePipeline", null, []), values, "consecutiveFailures >= 3");
    }

    private static Mock<IPipelineDisableRecord> Record(bool succeeds = true)
    {
        var record = new Mock<IPipelineDisableRecord>();
        record.Setup(r => r.Write(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<PipelineRunOutcome>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(succeeds ? GenericResult.Success() : GenericResult.Failure(new GenericMessage("the event was refused")));
        return record;
    }

    private static PipelineDisabler Disabler(Configurations configurations, Mock<IPipelineDisableRecord> record) =>
        new(configurations, record.Object, new FixedTime(Now));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ThePipelineIsDisabledMarkedAndTheReasonRecordedOnTheRun()
    {
        var configurations = new Configurations(new GraphPipelineConfiguration { Name = "Orders", IsEnabled = true });
        var record = Record();

        var result = await Disabler(configurations, record).Disable(Firing(Run, Failure(3), Failure(2), Failure(1)), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var saved = configurations.Saved.ShouldNotBeNull();
        saved.IsEnabled.ShouldBeFalse();
        saved.DisabledByFailureRuleAt.ShouldBe(Now);
        saved.DisabledByFailureRuleRunId.ShouldBe(Run);
        record.Verify(r => r.Write(Run, "Orders", 3, "consecutiveFailures >= 3", It.Is<IReadOnlyList<PipelineRunOutcome>>(failures => failures.Count == 3), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task APipelineThatIsAlreadyDisabledIsLeftAlone()
    {
        var configurations = new Configurations(new GraphPipelineConfiguration { Name = "Orders", IsEnabled = false });
        var record = Record();

        var result = await Disabler(configurations, record).Disable(Firing(Run, Failure(1)), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        configurations.Saved.ShouldBeNull();
        record.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARecordThatCannotBeWrittenStillDisablesThePipelineAndFailsTheConsequence()
    {
        var configurations = new Configurations(new GraphPipelineConfiguration { Name = "Orders", IsEnabled = true });

        var result = await Disabler(configurations, Record(succeeds: false)).Disable(Firing(Run, Failure(1)), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        configurations.Saved.ShouldNotBeNull().IsEnabled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ASaveThatIsRefusedFailsTheConsequence()
    {
        var configurations = new Configurations(new GraphPipelineConfiguration { Name = "Orders", IsEnabled = true }, disableSucceeds: false);

        var result = await Disabler(configurations, Record()).Disable(Firing(Run, Failure(1)), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AFiringWithNoRunDisablesNothing()
    {
        var configurations = new Configurations(new GraphPipelineConfiguration { Name = "Orders", IsEnabled = true });

        var result = await Disabler(configurations, Record()).Disable(Firing(null, Failure(1)), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        configurations.Saved.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AFiringThatCarriesNoCountedFailuresDisablesNothing()
    {
        var configurations = new Configurations(new GraphPipelineConfiguration { Name = "Orders", IsEnabled = true });
        var firing = Firing(Run) with { Values = new Dictionary<string, object?>() };

        var result = await Disabler(configurations, Record()).Disable(firing, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        configurations.Saved.ShouldBeNull();
    }
}
