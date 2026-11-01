using System;
using System.Collections.Generic;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Operations.Endpoints.Executions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Endpoints.Tests;

/// <summary>
/// Tests for <see cref="ExecutionSummaryResponse.Of"/> (FDW-932, FDW-933): a list row carries how the execution ended and what
/// started it, so a screen of failed runs can show each run's reason.
/// </summary>
[Trait("Priority", "P1")]
public class ExecutionSummaryResponseTests
{
    private static IExecutionItem Item(string? resultCode, string? resultMessage, Guid? parentId, string? triggerSource)
    {
        var itemType = new Mock<IExecutionItemType>();
        itemType.SetupGet(t => t.Name).Returns("Job");
        var state = new Mock<IExecutionStateType>();
        state.SetupGet(s => s.Name).Returns("Failed");

        var item = new Mock<IExecutionItem>();
        item.SetupGet(i => i.Id).Returns(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        item.SetupGet(i => i.ParentId).Returns(parentId);
        item.SetupGet(i => i.RootId).Returns(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        item.SetupGet(i => i.ItemType).Returns(itemType.Object);
        item.SetupGet(i => i.State).Returns(state.Object);
        item.SetupGet(i => i.Name).Returns("Orders_20261014195357");
        item.SetupGet(i => i.CorrelationId).Returns("corr-1");
        item.SetupGet(i => i.TriggerSource).Returns(triggerSource);
        item.SetupGet(i => i.CreatedAt).Returns(new DateTimeOffset(2026, 10, 14, 19, 53, 57, TimeSpan.Zero));
        item.SetupGet(i => i.StartedAt).Returns(new DateTimeOffset(2026, 10, 14, 19, 53, 58, TimeSpan.Zero));
        item.SetupGet(i => i.CompletedAt).Returns(new DateTimeOffset(2026, 10, 14, 19, 54, 2, TimeSpan.Zero));
        item.SetupGet(i => i.Parameters).Returns(new Dictionary<string, object?>());
        item.SetupGet(i => i.ResultCode).Returns(resultCode);
        item.SetupGet(i => i.ResultMessage).Returns(resultMessage);
        return item.Object;
    }

    [Fact]
    [Trait("Category", "DataIntegrity")]
    public void AFailedRunsSummaryCarriesItsResultCodeAndMessage()
    {
        var summary = ExecutionSummaryResponse.Of(Item("ExecutionFailed", "Connection 'ErcotDb' refused the login", null, "Api"));

        summary.State.ShouldBe("Failed");
        summary.ResultCode.ShouldBe("ExecutionFailed");
        summary.ResultMessage.ShouldBe("Connection 'ErcotDb' refused the login");
    }

    [Fact]
    [Trait("Category", "DataIntegrity")]
    public void ASummaryCarriesWhatStartedTheRunAndWhereItSitsInTheTree()
    {
        var parent = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var summary = ExecutionSummaryResponse.Of(Item("Success", null, parent, "Scheduler"));

        summary.TriggerSource.ShouldBe("Scheduler");
        summary.ParentId.ShouldBe(parent);
        summary.RootId.ShouldBe(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        summary.StartedAt.ShouldBe(new DateTimeOffset(2026, 10, 14, 19, 53, 58, TimeSpan.Zero));
        summary.CompletedAt.ShouldBe(new DateTimeOffset(2026, 10, 14, 19, 54, 2, TimeSpan.Zero));
    }

    [Fact]
    [Trait("Category", "DataIntegrity")]
    public void ARunWithNoRecordedReasonAnswersNullNotAnInventedOne()
    {
        var summary = ExecutionSummaryResponse.Of(Item(null, null, null, null));

        summary.ResultCode.ShouldBeNull();
        summary.ResultMessage.ShouldBeNull();
        summary.TriggerSource.ShouldBeNull();
        summary.ParentId.ShouldBeNull();
    }
}
