using System.Collections.Generic;
using System.Linq;
using Fdw.Messages;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.Thresholds;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Thresholds;
using Moq;
using static Fdw.Services.Etl.Tests.Thresholds.WaitThresholdKit;

namespace Fdw.Services.Etl.Tests.Thresholds;

/// <summary>
/// Tests for the gate that answers whether a run that arrives is taken (FDW-3975): a level that applies no thresholds takes every run; otherwise the evaluator is asked which
/// HoldNewRuns rules of the thresholds attached to the owner hold, with the waiting runs of that owner, and a rule that holds refuses the run; a run with no tenant, or a schedule
/// that is not one of the schedules read, cannot be asked about and is not taken.
/// </summary>
public sealed class WaitHoldGateTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid PipelineId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid NightlyId = Guid.Parse("33333333-3333-4333-8333-333333333333");

    private sealed class Evaluator(IReadOnlyList<ThresholdMatch> held) : IThresholdEvaluator
    {
        public ThresholdSubject? Subject { get; private set; }

        public IReadOnlyDictionary<string, object?>? Values { get; private set; }

        public string? Escalation { get; private set; }

        public Task<IGenericResult<ThresholdReport>> Report(ThresholdSubject subject, IReadOnlyDictionary<string, object?> values, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The gate never reports.");

        public Task<IGenericResult<ThresholdReport>> Judge(
            ThresholdSubject subject, IReadOnlyList<IThresholdDefinition> thresholds, IReadOnlyDictionary<string, object?> values, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The gate never judges.");

        public IReadOnlyList<ThresholdMatch> Holds(
            ThresholdSubject subject, IReadOnlyList<IThresholdDefinition> thresholds, IReadOnlyDictionary<string, object?> values, string escalation)
        {
            Subject = subject;
            Values = values;
            Escalation = escalation;
            return held;
        }
    }

    private readonly Mock<IThresholdSource> _source = new();

    private WaitHoldGate Gate(Evaluator evaluator)
    {
        _source.Setup(s => s.For(It.IsAny<ThresholdSubject>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IReadOnlyList<IThresholdDefinition>>.Success([]));
        return new WaitHoldGate(_source.Object, evaluator);
    }

    private static PipelineExecutionRequest Request(Guid? tenant, string? schedule = null, Guid? scheduleId = null) => new()
    {
        ExecutionId = Guid.NewGuid(), PipelineId = PipelineId, PipelineName = "Load", TriggerSource = "Api", ScheduleName = schedule, ScheduleId = scheduleId, TenantId = tenant,
        RunRoles = ["DataWriter"],
    };

    private static PipelineRunQueueEntry Waiting(Guid pipelineId, Guid tenant, string? schedule = null, Guid? scheduleId = null) =>
        new(new PipelineRunIdentity(Guid.NewGuid(), pipelineId, "Any", tenant, schedule, scheduleId), DateTimeOffset.UnixEpoch, null, null);

    private static PipelineRunQueueSnapshot Snapshot(params PipelineRunQueueEntry[] entries) => new([], entries);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ALevelThatAppliesNoThresholdsTakesTheRunWithoutAskingAnything()
    {
        var evaluator = new Evaluator([new ThresholdMatch("Full", 1, "HoldNewRuns", "full")]);

        var check = await Gate(evaluator).Check(Request(Tenant), Layer("Pipeline:Load", "None"), Snapshot(), TestContext.Current.CancellationToken);

        check.Value!.IsStopped.ShouldBeFalse();
        evaluator.Subject.ShouldBeNull();
        _source.Verify(s => s.For(It.IsAny<ThresholdSubject>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AHoldingRuleOfThePipelineRefusesTheRunNamingTheThresholdAndTheEvaluatorIsAskedForHoldNewRunsWithThePipelinesWaitingRuns()
    {
        var evaluator = new Evaluator([new ThresholdMatch("Queue full", 1, "HoldNewRuns", "Rule 1 of threshold 'Queue full' matched")]);
        var other = Guid.NewGuid();

        var check = await Gate(evaluator).Check(
            Request(Tenant), Layer("Pipeline:Load", "Own"), Snapshot(Waiting(PipelineId, Tenant), Waiting(PipelineId, Tenant), Waiting(other, Tenant)), TestContext.Current.CancellationToken);

        check.IsSuccess.ShouldBeTrue();
        check.Value!.IsStopped.ShouldBeTrue();
        check.Value.Threshold.ShouldBe("Queue full");
        check.Value.Reason.ShouldContain("Queue full");
        evaluator.Subject.ShouldBe(new ThresholdSubject("Pipeline", "Load", PipelineId, Tenant, null));
        evaluator.Escalation.ShouldBe("HoldNewRuns");
        evaluator.Values!["waitingRuns"].ShouldBe(2L);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task WhenNoRuleHoldsTheRunIsTaken()
    {
        var check = await Gate(new Evaluator([])).Check(Request(Tenant), Layer("Server", "Own"), Snapshot(Waiting(PipelineId, Tenant)), TestContext.Current.CancellationToken);

        check.Value!.IsStopped.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheServerIsAskedWithEveryWaitingRunAndAScheduleWithTheIdReadForItAndOnlyItsOwnRuns()
    {
        var server = new Evaluator([]);
        var schedule = new Evaluator([]);
        var other = Guid.NewGuid();
        var snapshot = Snapshot(Waiting(PipelineId, Tenant, "Nightly", NightlyId), Waiting(other, Tenant), Waiting(other, Tenant, "nightly", NightlyId), Waiting(other, Guid.NewGuid(), "Nightly", NightlyId));

        await Gate(server).Check(Request(Tenant), Layer("Server", "Own"), snapshot, TestContext.Current.CancellationToken);
        await Gate(schedule).Check(Request(Tenant, "Nightly", NightlyId), Layer("Schedule:Nightly", "Own"), snapshot, TestContext.Current.CancellationToken);

        server.Subject.ShouldBe(new ThresholdSubject("Server", "Server", Tenant, Tenant, null));
        server.Values!["waitingRuns"].ShouldBe(4L);
        schedule.Subject.ShouldBe(new ThresholdSubject("Schedule", "Nightly", NightlyId, Tenant, null));
        schedule.Values!["waitingRuns"].ShouldBe(2L);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunWithNoTenantAndAScheduleRunThatCarriesNoScheduleIdCannotBeAskedAboutAndAreNotTaken()
    {
        var gate = Gate(new Evaluator([]));

        var tenantless = await gate.Check(Request(null), Layer("Server", "Own"), Snapshot(), TestContext.Current.CancellationToken);
        var unknown = await gate.Check(Request(Tenant, "Gone"), Layer("Schedule:Gone", "Own"), Snapshot(), TestContext.Current.CancellationToken);

        tenantless.IsFailure.ShouldBeTrue();
        tenantless.CurrentMessage.ShouldNotBeNull().ShouldContain("Load");
        unknown.IsFailure.ShouldBeTrue();
        unknown.CurrentMessage.ShouldNotBeNull().ShouldContain("Gone");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AThresholdSourceThatFailsFailsTheCheckWithItsMessage()
    {
        var gate = Gate(new Evaluator([]));
        _source.Setup(s => s.For(It.IsAny<ThresholdSubject>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IThresholdDefinition>>.Failure(new GenericMessage("the thresholds cannot be read")));

        var check = await gate.Check(Request(Tenant), Layer("Pipeline:Load", "Own"), Snapshot(), TestContext.Current.CancellationToken);

        check.IsFailure.ShouldBeTrue();
        check.CurrentMessage.ShouldNotBeNull().ShouldContain("cannot be read");
    }
}
