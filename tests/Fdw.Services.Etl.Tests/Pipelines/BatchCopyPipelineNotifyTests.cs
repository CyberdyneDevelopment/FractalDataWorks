using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Pipelines;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Pipelines.Abstractions.DataDestination.Kinds;
using Fdw.Services.Pipelines.Abstractions.DataSource.Kinds;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Pipelines;

/// <summary>
/// <see cref="BatchCopyPipeline"/> fires its Notify stages once per run at the run point each states (FDW-1120): after the
/// final page, after the load, or when the run fails. The order of gateway calls and notifications is the behavior read.
/// </summary>
[Collection(EtlOperationalConnectionCollection.Name)]
public sealed class BatchCopyPipelineNotifyTests : IDisposable
{
    private const string OperationalStore = "Ops";

    private readonly List<string> _events = [];

    public BatchCopyPipelineNotifyTests()
    {
        EtlPipelineTypes.OperationalConnection = OperationalStore;
    }

    public void Dispose()
    {
        EtlPipelineTypes.OperationalConnection = null;
    }

    private static PipelineTransformConfiguration Notify(string name, int order, string runPoint, string rule, string message, bool enabled = true) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        OperationType = "Notify",
        ExecutionOrder = order,
        IsEnabled = enabled,
        RunPoint = runPoint,
        NotificationRuleName = rule,
        NotificationMessage = message
    };

    private static BatchCopyPipelineConfiguration Configuration(params PipelineTransformConfiguration[] stages) => new()
    {
        Id = Guid.NewGuid(),
        Name = "copy",
        BatchSize = 2,
        LoadMode = "Append",
        Transforms = stages,
        SourceKind = new DataSetKind(),
        SourceDataSet = "Source",
        DestinationKind = new DataSetDestinationKind(),
        DestinationDataSet = "Sink",
    };

    private LoadingGateway Gateway(int rows = 5, bool failLoads = false) => new(LoadingGateway.Rows(rows), _events, failLoads: failLoads);

    private static BatchCopyPipeline Pipeline(BatchCopyPipelineConfiguration configuration, LoadingGateway gateway, FakeRuleNotifier? notifier) =>
        new(configuration, NullLogger<BatchCopyPipeline>.Instance, gateway.Provider, ruleNotifier: notifier);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AfterStageFiresOnceAfterTheFinalPageAndAfterLoadOnceAfterThat()
    {
        // Arrange
        var notifier = new FakeRuleNotifier(_events);
        var configuration = Configuration(
            Notify("last", 2, "AfterLoad", "ops", "loaded"),
            Notify("first", 1, "AfterStage", "ops", "staged"));

        // Act
        var outcome = await Pipeline(configuration, Gateway(), notifier).Execute(TestContext.Current.CancellationToken);

        // Assert: once each, after every page was read and loaded, never per page
        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        _events.ShouldBe(new[]
        {
            "read@0", "insert:2", "read@2", "insert:2", "read@4", "insert:1",
            "notify:ops:staged", "notify:ops:loaded",
        });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheRecordsAreLoadedExactlyAsWithoutNotifyStages()
    {
        // Arrange
        var notifier = new FakeRuleNotifier(_events);
        var configuration = Configuration(Notify("tell", 1, "AfterLoad", "ops", "done"));

        // Act
        var outcome = await Pipeline(configuration, Gateway(), notifier).Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        outcome.Value!.RecordsExtracted.ShouldBe(5);
        outcome.Value.RecordsLoaded.ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task OnFailureFiresWhenTheLoadFailsAndTheOtherRunPointsDoNot()
    {
        // Arrange
        var notifier = new FakeRuleNotifier(_events);
        var configuration = Configuration(
            Notify("staged", 1, "AfterStage", "ops", "staged"),
            Notify("loaded", 2, "AfterLoad", "ops", "loaded"),
            Notify("broke", 3, "OnFailure", "ops", "failed"));

        // Act
        var outcome = await Pipeline(configuration, Gateway(failLoads: true), notifier).Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeFalse();
        _events.ShouldBe(new[] { "read@0", "insert:2", "notify:ops:failed" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task OnFailureDoesNotFireWhenTheRunSucceeds()
    {
        // Arrange
        var notifier = new FakeRuleNotifier(_events);
        var configuration = Configuration(Notify("broke", 1, "OnFailure", "ops", "failed"));

        // Act
        var outcome = await Pipeline(configuration, Gateway(), notifier).Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        notifier.Sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task SeveralStagesAtOneRunPointAllFireInStageOrder()
    {
        // Arrange
        var notifier = new FakeRuleNotifier(_events);
        var configuration = Configuration(
            Notify("second", 2, "AfterLoad", "pager", "two"),
            Notify("first", 1, "AfterLoad", "ops", "one"));

        // Act
        var outcome = await Pipeline(configuration, Gateway(rows: 1), notifier).Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        notifier.Sent.Select(sent => sent.RuleName).ShouldBe(new[] { "ops", "pager" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheMessageCarriesTheRunsFactsAndTheNotificationIsScopedToThePipeline()
    {
        // Arrange
        var notifier = new FakeRuleNotifier(_events);
        var configuration = Configuration(Notify("tell", 1, "AfterLoad", "ops", "{{PipelineName}}: {{RowsExtracted}} read, {{RowsRejected}} rejected, {{RowsLoaded}} loaded"));

        // Act
        var outcome = await Pipeline(configuration, Gateway(), notifier).Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        var sent = notifier.Sent.Single();
        sent.Message.ShouldBe("copy: 5 read, 0 rejected, 5 loaded");
        sent.PipelineId.ShouldBe(configuration.Id);
        sent.CorrelationId.ShouldBe(outcome.Value!.ExecutionId.ToString());
        sent.Metadata["RowsLoaded"].ShouldBe("5");
        sent.Metadata["RunPoint"].ShouldBe("AfterLoad");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ADisabledNotifyStageSendsNothing()
    {
        // Arrange
        var notifier = new FakeRuleNotifier(_events);
        var configuration = Configuration(Notify("tell", 1, "AfterLoad", "ops", "done", enabled: false));

        // Act
        var outcome = await Pipeline(configuration, Gateway(), notifier).Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        notifier.Sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ADryRunChecksTheStagesButSendsNothing()
    {
        // Arrange
        var notifier = new FakeRuleNotifier(_events);
        var configuration = Configuration(Notify("tell", 1, "AfterLoad", "ops", "done"));

        // Act
        var outcome = await Pipeline(configuration, Gateway(), notifier).Execute(PipelineExecutionOptions.DefaultTestMode, TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        notifier.Sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AHostWithNoNotifierRefusesTheRunBeforeReadingAnything()
    {
        // Arrange
        var configuration = Configuration(Notify("tell", 1, "AfterLoad", "ops", "done"));

        // Act
        var outcome = await Pipeline(configuration, Gateway(), notifier: null).Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeFalse();
        outcome.CurrentMessage.ShouldNotBeNull().ShouldContain("tell");
        _events.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARuleThatNoLongerExistsRefusesTheRunBeforeReadingAnything()
    {
        // Arrange
        var notifier = new FakeRuleNotifier(_events);
        notifier.MissingRules.Add("gone");
        var configuration = Configuration(Notify("tell", 1, "OnFailure", "gone", "done"));

        // Act
        var outcome = await Pipeline(configuration, Gateway(), notifier).Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeFalse();
        outcome.CurrentMessage.ShouldNotBeNull().ShouldContain("gone");
        _events.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AMessageWithAnUnknownVariableRefusesTheRunBeforeReadingAnything()
    {
        // Arrange
        var notifier = new FakeRuleNotifier(_events);
        var configuration = Configuration(Notify("tell", 1, "AfterLoad", "ops", "{{Mood}}"));

        // Act
        var outcome = await Pipeline(configuration, Gateway(), notifier).Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeFalse();
        outcome.CurrentMessage.ShouldNotBeNull().ShouldContain("Mood");
        _events.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ANotificationThatCannotBeSentFailsTheRunAndFiresOnFailure()
    {
        // Arrange
        var notifier = new FakeRuleNotifier(_events);
        notifier.FailingRules.Add("pager");
        var configuration = Configuration(
            Notify("tell", 1, "AfterLoad", "pager", "done"),
            Notify("broke", 2, "OnFailure", "ops", "failed"));

        // Act
        var outcome = await Pipeline(configuration, Gateway(rows: 1), notifier).Execute(TestContext.Current.CancellationToken);

        // Assert
        outcome.IsSuccess.ShouldBeFalse();
        outcome.CurrentMessage.ShouldNotBeNull().ShouldContain("pager");
        _events.ShouldBe(new[] { "read@0", "insert:1", "notify:pager:done", "notify:ops:failed" });
    }
}
