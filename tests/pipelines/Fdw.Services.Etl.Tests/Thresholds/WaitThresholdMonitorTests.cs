using System.Collections.Generic;
using System.Linq;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.Thresholds;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Thresholds;
using Fdw.Services.Notifications.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static Fdw.Services.Etl.Tests.Thresholds.WaitThresholdKit;

namespace Fdw.Services.Etl.Tests.Thresholds;

/// <summary>
/// Tests for the monitor of thresholds on waiting runs (FDW-3161): the notification rules of a threshold are sent once when it begins to fire and again only after
/// it has cleared; a run that arrives is stopped only by a firing threshold that stops new runs and applies to its schedule and pipeline; and a configuration
/// that cannot be read, or a pipeline it does not know, fails with nothing assumed.
/// </summary>
public sealed class WaitThresholdMonitorTests
{
    private sealed class Source(IGenericResult<WaitThresholdConfiguration> result) : IWaitThresholdSource
    {
        public Task<IGenericResult<WaitThresholdConfiguration>> Read(CancellationToken cancellationToken = default) => Task.FromResult(result);
    }

    private readonly Mock<IRuleNotifier> _notifier = new();
    private readonly List<RuleNotification> _sent = [];

    private WaitThresholdMonitor Monitor(IGenericResult<WaitThresholdConfiguration> configuration)
    {
        _notifier
            .Setup(n => n.Notify(It.IsAny<RuleNotification>(), It.IsAny<CancellationToken>()))
            .Callback((RuleNotification notification, CancellationToken _) => _sent.Add(notification))
            .ReturnsAsync(GenericResult.Success());
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupProperty(a => a.Current);
        var scopes = new ServiceCollection().AddSingleton(accessor.Object).AddSingleton(_notifier.Object).BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        return new WaitThresholdMonitor(new Source(configuration), scopes, new WaitThresholdJudge(), NullLogger<WaitThresholdMonitor>.Instance);
    }

    private static IGenericResult<WaitThresholdConfiguration> Configuration(WaitThresholdLayer server, params (string Pipeline, WaitThresholdLayer Layer)[] pipelines) =>
        GenericResult<WaitThresholdConfiguration>.Success(new WaitThresholdConfiguration(
            server,
            pipelines.ToDictionary(p => p.Pipeline, p => p.Layer, StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, WaitThresholdLayer>(StringComparer.OrdinalIgnoreCase)));

    private static PipelineRunQueueSnapshot Waiting(string pipeline, int count) => new(
        [],
        [.. Enumerable.Range(0, count).Select(_ => new PipelineRunQueueEntry(new PipelineRunIdentity(Guid.NewGuid(), Guid.NewGuid(), pipeline, null), DateTimeOffset.UnixEpoch, null, null))]);

    private static PipelineExecutionRequest Request(string pipeline, string? schedule = null) => new()
    {
        ExecutionId = Guid.NewGuid(), PipelineId = Guid.NewGuid(), PipelineName = pipeline, TriggerSource = "Api", ScheduleName = schedule, RunRoles = ["DataWriter"],
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AThresholdThatBeginsToFireSendsEachOfItsNotificationRulesOnceWhileTheQueueStaysOverTheNumber()
    {
        var queueLong = Threshold("Queue getting long", 3, rules: ["Ops email", "Ops Teams"]);
        var monitor = Monitor(Configuration(Layer("Server", "Own", queueLong), ("P", Layer("Pipeline:P", "Inherit"))));

        await monitor.Evaluate(Waiting("P", 3), TestContext.Current.CancellationToken);
        await monitor.Evaluate(Waiting("P", 4), TestContext.Current.CancellationToken);
        await monitor.Evaluate(Waiting("P", 9), TestContext.Current.CancellationToken);

        _sent.Select(n => n.RuleName).ShouldBe(["Ops email", "Ops Teams"]);
        _sent[0].Subject.ShouldContain("Queue getting long");
        _sent[0].Metadata["WaitingRuns"].ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AThresholdThatClearedSendsItsRulesAgainWhenItFiresAgain()
    {
        var queueLong = Threshold("Queue getting long", 3, rules: ["Ops email"]);
        var monitor = Monitor(Configuration(Layer("Server", "Own", queueLong), ("P", Layer("Pipeline:P", "Inherit"))));

        await monitor.Evaluate(Waiting("P", 3), TestContext.Current.CancellationToken);
        await monitor.Evaluate(Waiting("P", 0), TestContext.Current.CancellationToken);
        await monitor.Evaluate(Waiting("P", 5), TestContext.Current.CancellationToken);

        _sent.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunIsStoppedOnlyWhileAThresholdThatStopsNewRunsFiresAndNamesItInTheReason()
    {
        var full = Threshold("Queue full", 3, stops: true, rules: ["Ops email"]);
        var monitor = Monitor(Configuration(Layer("Server", "Own", full), ("P", Layer("Pipeline:P", "Inherit"))));

        var before = await monitor.Check(Request("P"), TestContext.Current.CancellationToken);
        monitor.WaitingChanged(Waiting("P", 3));
        var during = await monitor.Check(Request("P"), TestContext.Current.CancellationToken);
        monitor.WaitingChanged(Waiting("P", 0));
        var after = await monitor.Check(Request("P"), TestContext.Current.CancellationToken);

        before.Value!.IsStopped.ShouldBeFalse();
        during.Value!.IsStopped.ShouldBeTrue();
        during.Value.Threshold.ShouldBe("Queue full");
        during.Value.Reason.ShouldContain("Queue full");
        after.Value!.IsStopped.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AFiringThresholdThatOnlyNotifiesNeverStopsARun()
    {
        var lengthy = Threshold("Queue getting long", 3, rules: ["Ops email"]);
        var monitor = Monitor(Configuration(Layer("Server", "Own", lengthy), ("P", Layer("Pipeline:P", "Inherit"))));
        monitor.WaitingChanged(Waiting("P", 5));

        var check = await monitor.Check(Request("P"), TestContext.Current.CancellationToken);

        check.Value!.IsStopped.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task APipelineThatStatesNoneIsNotStoppedByTheServersThreshold()
    {
        var full = Threshold("Queue full", 3, stops: true, rules: ["Ops email"]);
        var monitor = Monitor(Configuration(Layer("Server", "Own", full), ("Quiet", Layer("Pipeline:Quiet", "None")), ("P", Layer("Pipeline:P", "Inherit"))));
        monitor.WaitingChanged(Waiting("P", 5));

        var quiet = await monitor.Check(Request("Quiet"), TestContext.Current.CancellationToken);
        var other = await monitor.Check(Request("P"), TestContext.Current.CancellationToken);

        quiet.Value!.IsStopped.ShouldBeFalse();
        other.Value!.IsStopped.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task APipelineTheConfigurationDoesNotKnowFailsNamingItAndIsNeverAssumedToBeOpen()
    {
        var monitor = Monitor(Configuration(Layer("Server", "None")));

        var check = await monitor.Check(Request("Unknown"), TestContext.Current.CancellationToken);

        check.IsFailure.ShouldBeTrue();
        check.CurrentMessage.ShouldNotBeNull().ShouldContain("Unknown");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AConfigurationThatCannotBeReadFailsTheCheckAndSendsNothingWhenWaitingRunsAreJudged()
    {
        var monitor = Monitor(GenericResult<WaitThresholdConfiguration>.Failure(new GenericMessage("the server states no thresholds row")));

        var check = await monitor.Check(Request("P"), TestContext.Current.CancellationToken);
        await monitor.Evaluate(Waiting("P", 50), TestContext.Current.CancellationToken);

        check.IsFailure.ShouldBeTrue();
        _sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARuleThatCannotBeSentDoesNotStopTheOthersFromBeingSent()
    {
        var queueLong = Threshold("Queue getting long", 1, rules: ["Broken", "Ops email"]);
        var monitor = Monitor(Configuration(Layer("Server", "Own", queueLong), ("P", Layer("Pipeline:P", "Inherit"))));
        _notifier
            .Setup(n => n.Notify(It.Is<RuleNotification>(notification => notification.RuleName == "Broken"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new GenericMessage("no such rule")));

        await monitor.Evaluate(Waiting("P", 2), TestContext.Current.CancellationToken);

        _sent.Select(n => n.RuleName).ShouldBe(["Ops email"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AStoppingThresholdSavedWhileItsConditionAlreadyHoldsStopsTheVeryNextRunWithNoChangeJudgedBetween()
    {
        var monitor = Monitor(Configuration(Layer("Server", "None"), ("P", Layer("Pipeline:P", "Own", Threshold("Full", 3, stops: true, owner: "Pipeline:P", pipeline: "P")))));
        monitor.WaitingChanged(Waiting("P", 3));

        var next = await monitor.Check(Request("P"), TestContext.Current.CancellationToken);

        next.Value!.IsStopped.ShouldBeTrue();
        next.Value.Threshold.ShouldBe("Full");
    }
}
