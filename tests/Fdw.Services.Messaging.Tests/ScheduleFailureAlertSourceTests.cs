using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Operations.Logging;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Messaging.Tests;

/// <summary>
/// FDW-2431: a failed run is an alert of the ScheduleFailure kind. The tracker is a double, so these tests are about what the source decides: which runs are
/// alerts, who may see them, and what it does when the tracker cannot answer.
/// </summary>
public sealed class ScheduleFailureAlertSourceTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private sealed class StubAccessor : IAuthenticationContextAccessor
    {
        public IAuthenticationContext? Current { get; set; }
    }

    private static StubAccessor SignedIn(params string[] permissions)
    {
        var context = new Mock<IAuthenticationContext>(MockBehavior.Loose);
        context.SetupGet(c => c.IsAuthenticated).Returns(true);
        context.SetupGet(c => c.Permissions).Returns(permissions);
        return new StubAccessor { Current = context.Object };
    }

    private static IExecutionItem Item(Guid id, IExecutionItemType type, IExecutionStateType state)
    {
        var item = new Mock<IExecutionItem>(MockBehavior.Loose);
        item.SetupGet(i => i.Id).Returns(id);
        item.SetupGet(i => i.ItemType).Returns(type);
        item.SetupGet(i => i.State).Returns(state);
        item.SetupGet(i => i.TenantId).Returns(Tenant);
        return item.Object;
    }

    private static Mock<IExecutionTracker> Tracker(Guid id, IGenericResult<IExecutionItem> answer)
    {
        var tracker = new Mock<IExecutionTracker>(MockBehavior.Strict);
        tracker.Setup(t => t.GetItem(id, It.IsAny<CancellationToken>())).ReturnsAsync(answer);
        return tracker;
    }

    private static ScheduleFailureAlertSource Source(StubAccessor accessor, IExecutionTracker? tracker)
        => new(NullLogger<ScheduleFailureAlertSource>.Instance, accessor, tracker);

    [Fact]
    public void TheKindIsScheduleFailureAndItOffersTheFiveDefaultActionsInOrder()
    {
        var source = Source(SignedIn("pipelines:read"), null);

        source.Kind.ShouldBe("ScheduleFailure");
        source.OfferedActions.ShouldBe(["Acknowledge", "Snooze", "Silence", "Dismiss", "Discard"]);
    }

    [Fact]
    public async Task AFailedRunIsAnAlertOfTheTenantItBelongsTo()
    {
        var id = Guid.NewGuid();
        var tracker = Tracker(id, GenericResult<IExecutionItem>.Success(Item(id, ExecutionItemTypes.Job, ExecutionStateTypes.Failed)));

        var found = await Source(SignedIn("pipelines:read"), tracker.Object).Find(id, TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue(found.CurrentMessage);
        var subject = found.Value.ShouldNotBeNull();
        subject.Id.ShouldBe(id);
        subject.TenantId.ShouldBe(Tenant);
    }

    [Fact]
    public async Task ARunThatHasNotFailedIsNotAnAlert()
    {
        var id = Guid.NewGuid();
        var tracker = Tracker(id, GenericResult<IExecutionItem>.Success(Item(id, ExecutionItemTypes.Job, ExecutionStateTypes.Running)));

        var found = await Source(SignedIn("pipelines:read"), tracker.Object).Find(id, TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue(found.CurrentMessage);
        found.Value.ShouldBeNull();
    }

    [Fact]
    public async Task AFailedStageOfARunIsNotAnAlertBecauseTheListHoldsRuns()
    {
        var id = Guid.NewGuid();
        var tracker = Tracker(id, GenericResult<IExecutionItem>.Success(Item(id, ExecutionItemTypes.Stage, ExecutionStateTypes.Failed)));

        var found = await Source(SignedIn("pipelines:read"), tracker.Object).Find(id, TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue(found.CurrentMessage);
        found.Value.ShouldBeNull();
    }

    [Fact]
    public async Task ACallerWithoutTheRightToReadFailuresDoesNotSeeTheAlertAndTheTrackerIsNotAsked()
    {
        var tracker = new Mock<IExecutionTracker>(MockBehavior.Strict);

        var found = await Source(SignedIn("ui-feedback:read"), tracker.Object).Find(Guid.NewGuid(), TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue(found.CurrentMessage);
        found.Value.ShouldBeNull();
        tracker.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task NoSignedInCallerSeesNothing()
    {
        var tracker = new Mock<IExecutionTracker>(MockBehavior.Strict);

        var found = await Source(new StubAccessor(), tracker.Object).Find(Guid.NewGuid(), TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue(found.CurrentMessage);
        found.Value.ShouldBeNull();
    }

    [Fact]
    public async Task AnIdentifierThatNamesNoRunIsNotAnAlertAndNotAFailure()
    {
        var id = Guid.NewGuid();
        var tracker = Tracker(id, GenericResult<IExecutionItem>.Failure(OperationsLog.ExecutionItemNotFound(NullLogger.Instance, id)));

        var found = await Source(SignedIn("pipelines:read"), tracker.Object).Find(id, TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue(found.CurrentMessage);
        found.Value.ShouldBeNull();
    }

    [Fact]
    public async Task AFailureOfTheTrackerThatIsNotAMissingFailsTheLookup()
    {
        var id = Guid.NewGuid();
        var tracker = Tracker(id, GenericResult<IExecutionItem>.Failure(OperationsLog.OperationalConnectionNotSet(NullLogger.Instance)));

        var found = await Source(SignedIn("pipelines:read"), tracker.Object).Find(id, TestContext.Current.CancellationToken);

        found.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task AHostWithNoTrackerFailsTheLookupInsteadOfSayingThereIsNoAlert()
    {
        var found = await Source(SignedIn("pipelines:read"), null).Find(Guid.NewGuid(), TestContext.Current.CancellationToken);

        found.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task ActingOnAFailureChangesNothingAndSucceeds()
    {
        var tracker = new Mock<IExecutionTracker>(MockBehavior.Strict);
        var action = new Mock<IAlertAction>(MockBehavior.Loose);

        var acted = await Source(SignedIn("pipelines:read"), tracker.Object).Acted(Guid.NewGuid(), action.Object, TestContext.Current.CancellationToken);

        acted.IsSuccess.ShouldBeTrue(acted.CurrentMessage);
        tracker.VerifyNoOtherCalls();
    }
}
