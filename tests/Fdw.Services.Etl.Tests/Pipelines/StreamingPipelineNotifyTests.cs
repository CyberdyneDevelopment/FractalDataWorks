using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Pipelines;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Pipelines;

/// <summary>
/// <see cref="StreamingPipeline"/> fires its Notify stages once the stream has ended (FDW-1120): a stream has no final page of
/// its own, so AfterStage and AfterLoad both fire after the last flush has loaded.
/// </summary>
public sealed class StreamingPipelineNotifyTests
{
    private static PipelineTransformConfiguration Notify(string name, int order, string runPoint, string rule, string message) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        OperationType = "Notify",
        ExecutionOrder = order,
        IsEnabled = true,
        RunPoint = runPoint,
        NotificationRuleName = rule,
        NotificationMessage = message
    };

    private static StreamingPipelineConfiguration Configuration(params PipelineTransformConfiguration[] stages) => new()
    {
        Id = Guid.NewGuid(),
        Name = "stream",
        SourceDataSet = "Source",
        DestinationDataSet = "Sink",
        BufferSize = 2,
        FlushIntervalMs = 0,
        Transforms = stages,
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AfterStageAndAfterLoadFireOnceEachAfterTheStreamHasEnded()
    {
        // Arrange
        var events = new List<string>();
        var source = PagedSourceGateway.Rows(4);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var gateway = new PagedSourceGateway(source, events, onRead: skip =>
        {
            if (skip >= source.Count)
            {
                stop.Cancel();
            }
        });
        var notifier = new FakeRuleNotifier(events);
        var configuration = Configuration(
            Notify("staged", 1, "AfterStage", "ops", "staged {{RowsLoaded}}"),
            Notify("loaded", 2, "AfterLoad", "ops", "loaded {{RowsLoaded}}"),
            Notify("broke", 3, "OnFailure", "ops", "failed"));
        var pipeline = new StreamingPipeline(configuration, NullLogger<StreamingPipeline>.Instance, gateway.Provider, ruleNotifier: notifier);

        // Act
        var outcome = await pipeline.Execute(stop.Token);

        // Assert
        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        events.ShouldBe(new[] { "read@0", "load:2", "read@2", "load:2", "read@4", "notify:ops:staged 4", "notify:ops:loaded 4" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task OnFailureFiresWhenTheSourceCannotBeReadAndTheOtherRunPointsDoNot()
    {
        // Arrange
        var events = new List<string>();
        var gateway = new PagedSourceGateway(PagedSourceGateway.Rows(4), events, failReads: true);
        var notifier = new FakeRuleNotifier(events);
        var configuration = Configuration(
            Notify("staged", 1, "AfterStage", "ops", "staged"),
            Notify("loaded", 2, "AfterLoad", "ops", "loaded"),
            Notify("broke", 3, "OnFailure", "ops", "failed"));
        var pipeline = new StreamingPipeline(configuration, NullLogger<StreamingPipeline>.Instance, gateway.Provider, ruleNotifier: notifier);

        // Act
        var outcome = await pipeline.Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeFalse();
        events.ShouldBe(new[] { "read@0", "notify:ops:failed" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AHostWithNoNotifierRefusesTheRunBeforeReadingAnything()
    {
        // Arrange
        var events = new List<string>();
        var gateway = new PagedSourceGateway(PagedSourceGateway.Rows(4), events);
        var pipeline = new StreamingPipeline(
            Configuration(Notify("tell", 1, "AfterLoad", "ops", "done")), NullLogger<StreamingPipeline>.Instance, gateway.Provider);

        // Act
        var outcome = await pipeline.Execute(PipelineExecutionOptions.DefaultTestMode, TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeFalse();
        outcome.CurrentMessage.ShouldNotBeNull().ShouldContain("tell");
        events.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ADryRunSendsNothing()
    {
        // Arrange
        var events = new List<string>();
        var gateway = new PagedSourceGateway(PagedSourceGateway.Rows(4), events);
        var notifier = new FakeRuleNotifier(events);
        var pipeline = new StreamingPipeline(
            Configuration(Notify("tell", 1, "AfterLoad", "ops", "done")), NullLogger<StreamingPipeline>.Instance, gateway.Provider, ruleNotifier: notifier);

        // Act
        var outcome = await pipeline.Execute(PipelineExecutionOptions.DefaultTestMode, TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        notifier.Sent.ShouldBeEmpty();
    }
}
