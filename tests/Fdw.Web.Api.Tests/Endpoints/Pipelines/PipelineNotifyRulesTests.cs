using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Etl.Pipelines;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// Tests for the Notify stage on the pipeline API (FDW-1120, contract v5): its notification (rule, run point, message) is
/// mapped by the rules that map every stage, another kind of stage may not state one, a rule that does not exist is refused
/// where the stage sits (nested stages included), and a saved Notify stage reads back what it states.
/// </summary>
public sealed class PipelineNotifyRulesTests
{
    private const char Separator = '\u001F';

    private static CreatePipelineTransformRequest Notify(string name, string runPoint = "AfterLoad", string rule = "ops-alerts", string message = "done", int order = 1) =>
        new()
        {
            Name = name,
            OperationType = "Notify",
            ExecutionOrder = order,
            Notification = new PipelineNotification { RuleName = rule, RunPoint = runPoint, Message = message }
        };

    private static CreatePipelineTransformRequest Branch(string name, string path, params CreatePipelineTransformRequest[] stages) =>
        new()
        {
            Name = name,
            OperationType = "Branch",
            Unmatched = "Pass",
            ExecutionOrder = 1,
            Paths = [new CreatePipelinePathRequest { Name = path, Condition = "Age >= 18", Stages = [.. stages] }]
        };

    private static string[] PathOf(Fdw.Results.IGenericResult result) =>
        result.Details!.GetValue<string>("StagePath")!.Split(Separator);

    private sealed class Notifier(params string[] missing) : IRuleNotifier
    {
        public Task<Fdw.Results.IGenericResult> Exists(string ruleName, System.Threading.CancellationToken cancellationToken = default) =>
            Task.FromResult(missing.Contains(ruleName, StringComparer.Ordinal)
                ? Fdw.Results.GenericResult.Failure(new Fdw.Messages.GenericMessage($"rule '{ruleName}' was not found"))
                : Fdw.Results.GenericResult.Success());

        public Task<Fdw.Results.IGenericResult> Notify(RuleNotification notification, System.Threading.CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("the check never sends");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapDispatchesToNotifyAndKeepsTheRuleTheRunPointAndTheMessage()
    {
        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy", [Notify("Tell", "OnFailure", "pager", "{{PipelineName}} failed")], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var stage = result.Value!.Single();
        stage.OperationType.ShouldBe("Notify");
        stage.RunPoint.ShouldBe("OnFailure");
        stage.NotificationRuleName.ShouldBe("pager");
        stage.NotificationMessage.ShouldBe("{{PipelineName}} failed");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AStageOfAnotherKindMayNotStateANotification()
    {
        // Arrange
        var filter = new CreatePipelineTransformRequest
        {
            Name = "Keep",
            OperationType = "Filter",
            FilterExpression = "Age >= 18",
            Notification = new PipelineNotification { RuleName = "ops-alerts", RunPoint = "AfterLoad", Message = "done" }
        };

        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy", [filter], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Details!.GetValue<string>("Reason").ShouldNotBeNull().ShouldContain("'notification' is not a member of a Filter stage");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ANotifyStageMayNotStateAMemberItDoesNotOwn()
    {
        // Arrange
        var notify = Notify("Tell");
        notify.FilterExpression = "Age >= 18";

        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy", [notify], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Details!.GetValue<string>("Reason").ShouldNotBeNull().ShouldContain("'filterExpression' is not a member of a Notify stage");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("", "AfterLoad", "done", "ruleName")]
    [InlineData("ops-alerts", "Sometime", "done", "not a run point")]
    [InlineData("ops-alerts", "AfterLoad", "", "message")]
    [InlineData("ops-alerts", "AfterLoad", "{{Mood}}", "unknown variables")]
    public void ANotifyStageThatIsIncompleteIsRefusedNamingTheStage(string rule, string runPoint, string message, string reason)
    {
        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy", [Notify("Tell", runPoint, rule, message)], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code!.Id.ShouldBe(20001);
        result.Details!.GetValue<string>("StepName").ShouldBe("Tell");
        result.Details!.GetValue<string>("Reason").ShouldNotBeNull().ShouldContain(reason);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ANotifyStageWithNoNotificationIsRefused()
    {
        // Arrange
        var notify = new CreatePipelineTransformRequest { Name = "Tell", OperationType = "Notify" };

        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy", [notify], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Details!.GetValue<string>("Reason").ShouldNotBeNull().ShouldContain("does not state a notification");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task CheckPassesWhenEveryRuleExistsNestedStagesIncluded()
    {
        // Arrange
        var mapped = PipelineTransformConfigurationMapper.Map(
            "BatchCopy", [Notify("Tell"), Branch("Route", "Adults", Notify("TellAdults", rule: "adult-alerts"))], NullLogger.Instance);

        // Act
        var result = await PipelineNotifyCheck.Check(mapped.Value!, new Notifier(), NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        mapped.IsSuccess.ShouldBeTrue(mapped.CurrentMessage);
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task CheckRefusesAMissingRuleWithThePathToTheStageThatNamesIt()
    {
        // Arrange
        var mapped = PipelineTransformConfigurationMapper.Map(
            "BatchCopy", [Branch("Route", "Adults", Notify("TellAdults", rule: "gone"))], NullLogger.Instance);

        // Act
        var result = await PipelineNotifyCheck.Check(mapped.Value!, new Notifier("gone"), NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code!.Id.ShouldBe(20001);
        PathOf(result).ShouldBe(new[] { "Route", "Adults", "TellAdults" });
        result.Details!.GetValue<string>("Reason").ShouldNotBeNull().ShouldContain("gone");
        PipelineProblems.OfStageRefusal(result, "checking the notification rules").Stage.ShouldBe("Route.Adults.TellAdults");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task CheckRefusesANotifyStageOnAHostThatCannotSend()
    {
        // Arrange
        var mapped = PipelineTransformConfigurationMapper.Map("BatchCopy", [Notify("Tell")], NullLogger.Instance);

        // Act
        var result = await PipelineNotifyCheck.Check(mapped.Value!, notifier: null, NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        PathOf(result).ShouldBe(new[] { "Tell" });
        result.Details!.GetValue<string>("Reason").ShouldNotBeNull().ShouldContain("cannot send");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CheckHasNothingToJudgeWhenThereAreNoNotifyStagesEvenOnAHostThatCannotSend()
    {
        // Arrange
        var mapped = PipelineTransformConfigurationMapper.Map(
            "BatchCopy", [new CreatePipelineTransformRequest { Name = "Keep", OperationType = "Filter", FilterExpression = "Age >= 18" }], NullLogger.Instance);

        // Act
        var result = await PipelineNotifyCheck.Check(mapped.Value!, notifier: null, NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASavedNotifyStageReadsBackWhatItStatesAndOtherStagesStateNoNotification()
    {
        // Arrange
        var notify = new PipelineTransformConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Tell",
            OperationType = "Notify",
            RunPoint = "AfterStage",
            NotificationRuleName = "ops-alerts",
            NotificationMessage = "{{RowsLoaded}} loaded"
        };
        var filter = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Keep", OperationType = "Filter", FilterExpression = "Age >= 18", ExecutionOrder = 2 };

        // Act
        var detail = PipelineDetailMapper.Map(new BatchCopyPipelineConfiguration { Name = "Load", Implementation = "BatchCopy", Transforms = [notify, filter] });

        // Assert
        var stage = detail.Transforms!.Single(transform => string.Equals(transform.Name, "Tell", StringComparison.Ordinal));
        var notification = stage.Notification.ShouldNotBeNull();
        notification.RuleName.ShouldBe("ops-alerts");
        notification.RunPoint.ShouldBe("AfterStage");
        notification.Message.ShouldBe("{{RowsLoaded}} loaded");
        detail.Transforms!.Single(transform => string.Equals(transform.Name, "Keep", StringComparison.Ordinal)).Notification.ShouldBeNull();
    }
}
