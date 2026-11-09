using System.Collections.Generic;
using System.Linq;
using Fdw.Messages;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.Thresholds;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Thresholds;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static Fdw.Services.Etl.Tests.Thresholds.WaitThresholdKit;

namespace Fdw.Services.Etl.Tests.Thresholds;

/// <summary>
/// Tests for the monitor of thresholds on waiting runs (FDW-3161, FDW-3975): a run that arrives is taken or refused by the owner the levels choose and the gate; a level
/// configuration that cannot be read, or a pipeline or schedule it does not know, fails with nothing assumed; and the waiting runs it is told of reach the threshold evaluator.
/// </summary>
public sealed class WaitThresholdMonitorTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-4111-8111-111111111111");

    private sealed class Source(IGenericResult<WaitThresholdConfiguration> result) : IWaitThresholdSource
    {
        public Task<IGenericResult<WaitThresholdConfiguration>> Read(CancellationToken cancellationToken = default) => Task.FromResult(result);
    }

    private sealed class Evaluator(IReadOnlyList<ThresholdMatch> held) : IThresholdEvaluator
    {
        public List<(ThresholdSubject Subject, IReadOnlyDictionary<string, object?> Values)> Reports { get; } = [];

        public List<ThresholdSubject> Asked { get; } = [];

        public Task<IGenericResult<ThresholdReport>> Report(ThresholdSubject subject, IReadOnlyDictionary<string, object?> values, CancellationToken cancellationToken = default)
        {
            Reports.Add((subject, values));
            return Task.FromResult<IGenericResult<ThresholdReport>>(GenericResult<ThresholdReport>.Success(new ThresholdReport(0, [], [])));
        }

        public Task<IGenericResult<ThresholdReport>> Judge(
            ThresholdSubject subject, IReadOnlyList<IThresholdDefinition> thresholds, IReadOnlyDictionary<string, object?> values, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The monitor never judges a held set.");

        public IReadOnlyList<ThresholdMatch> Holds(
            ThresholdSubject subject, IReadOnlyList<IThresholdDefinition> thresholds, IReadOnlyDictionary<string, object?> values, string escalation)
        {
            Asked.Add(subject);
            return held;
        }
    }

    private readonly Evaluator _evaluator = new([new ThresholdMatch("Queue full", 1, "HoldNewRuns", "Rule 1 matched")]);

    private WaitThresholdMonitor Monitor(IGenericResult<WaitThresholdConfiguration> configuration)
    {
        var thresholds = new Mock<IThresholdSource>();
        thresholds.Setup(s => s.For(It.IsAny<ThresholdSubject>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IReadOnlyList<IThresholdDefinition>>.Success([]));
        return new WaitThresholdMonitor(
            new Source(configuration), new WaitHoldGate(WaitThresholdKit.Scopes(thresholds.Object, _evaluator)), new WaitThresholdReporter(WaitThresholdKit.Scopes(thresholds.Object, _evaluator)),
            NullLogger<WaitThresholdMonitor>.Instance);
    }

    private static IGenericResult<WaitThresholdConfiguration> Configuration(WaitThresholdLayer server, params (string Pipeline, WaitThresholdLayer Layer)[] pipelines) =>
        GenericResult<WaitThresholdConfiguration>.Success(new WaitThresholdConfiguration(
            server,
            pipelines.ToDictionary(p => p.Pipeline, p => p.Layer, StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, WaitThresholdLayer>(StringComparer.OrdinalIgnoreCase) { ["Nightly"] = Layer("Schedule:Nightly", "Inherit") }));

    private static readonly Guid PipelineId = Guid.Parse("22222222-2222-4222-8222-222222222222");

    private static PipelineRunQueueSnapshot Waiting(string pipeline, int count) => new(
        [],
        [.. Enumerable.Range(0, count).Select(_ => new PipelineRunQueueEntry(new PipelineRunIdentity(Guid.NewGuid(), PipelineId, pipeline, Tenant), DateTimeOffset.UnixEpoch, null, null))]);

    private static PipelineExecutionRequest Request(string pipeline, string? schedule = null) => new()
    {
        ExecutionId = Guid.NewGuid(), PipelineId = Guid.NewGuid(), PipelineName = pipeline, TriggerSource = "Api", ScheduleName = schedule, TenantId = Tenant, RunRoles = ["DataWriter"],
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AHoldingRuleOfTheOwnerTheLevelsChooseRefusesTheRunAndTheQueueTheMonitorWasToldOfIsWhatItCounts()
    {
        var monitor = Monitor(Configuration(Layer("Server", "Own"), ("P", Layer("Pipeline:P", "Inherit"))));
        monitor.WaitingChanged(Waiting("P", 3));

        var check = await monitor.Check(Request("P"), TestContext.Current.CancellationToken);

        check.Value!.IsStopped.ShouldBeTrue();
        check.Value.Threshold.ShouldBe("Queue full");
        _evaluator.Asked.Single().ShouldBe(new ThresholdSubject("Server", "Server", Tenant, Tenant, null));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task APipelineWhoseLevelAppliesNoThresholdsIsTakenWithoutAskingTheEvaluator()
    {
        var monitor = Monitor(Configuration(Layer("Server", "Own"), ("P", Layer("Pipeline:P", "None"))));

        var check = await monitor.Check(Request("P"), TestContext.Current.CancellationToken);

        check.Value!.IsStopped.ShouldBeFalse();
        _evaluator.Asked.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AConfigurationThatCannotBeReadOrAPipelineOrScheduleItDoesNotKnowFailsWithNothingAssumed()
    {
        var unreadable = await Monitor(GenericResult<WaitThresholdConfiguration>.Failure(new GenericMessage("the server states no level"))).Check(Request("P"), TestContext.Current.CancellationToken);
        var monitor = Monitor(Configuration(Layer("Server", "Own"), ("P", Layer("Pipeline:P", "Inherit"))));
        var pipeline = await monitor.Check(Request("Unknown"), TestContext.Current.CancellationToken);
        var schedule = await monitor.Check(Request("P", "Gone"), TestContext.Current.CancellationToken);

        unreadable.IsFailure.ShouldBeTrue();
        unreadable.CurrentMessage.ShouldNotBeNull().ShouldContain("no level");
        pipeline.IsFailure.ShouldBeTrue();
        pipeline.CurrentMessage.ShouldNotBeNull().ShouldContain("Unknown");
        schedule.IsFailure.ShouldBeTrue();
        schedule.CurrentMessage.ShouldNotBeNull().ShouldContain("Gone");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheWaitingRunsTheMonitorEvaluatesAreReportedToTheThresholdEvaluator()
    {
        var monitor = Monitor(Configuration(Layer("Server", "Own")));

        await monitor.Evaluate(Waiting("P", 2), TestContext.Current.CancellationToken);

        _evaluator.Reports.Select(report => report.Subject.Kind).Order().ShouldBe(["Pipeline", "Server"]);
        _evaluator.Reports.Single(report => report.Subject.Kind == "Server").Values["waitingRuns"].ShouldBe(2L);
    }
}
