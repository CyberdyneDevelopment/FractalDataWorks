using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.Thresholds;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for the queue of pipeline triggers (FDW-3155): it holds the number its setting gives and drops the trigger past it, and a host whose
/// setting is missing or not positive does not get a queue.
/// </summary>
public sealed class PipelineExecutionQueueTests
{
    private static PipelineExecutionRequest Request() => new()
    {
        ExecutionId = Guid.NewGuid(),
        PipelineId = Guid.NewGuid(),
        PipelineName = "Monthly",
        TriggerSource = "Test",
        RunRoles = ["DataWriter"]
    };

    // A store that records every request it is given and answers that it did, or fails every one.
    private static Mock<IPipelineRunRequestStore> Store(bool records = true)
    {
        var store = new Mock<IPipelineRunRequestStore>();
        store.Setup(s => s.Record(It.IsAny<PipelineExecutionRequest>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult<IGenericResult>(
            records ? GenericResult.Success() : GenericResult.Failure(new GenericMessage("the record is not available"))));
        return store;
    }

    // A stop check that takes every run, or one that stops every run with a reason, or one that cannot decide.
    private static IPipelineRunStopCheck Stops(IGenericResult<PipelineRunStopCheck>? answer = null)
    {
        var stops = new Mock<IPipelineRunStopCheck>();
        stops.Setup(s => s.Check(It.IsAny<PipelineExecutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(answer ?? GenericResult<PipelineRunStopCheck>.Success(PipelineRunStopCheck.Open));
        return stops.Object;
    }

    private static ServiceProvider Host(int capacity, Mock<IAuthenticationContextAccessor>? accessor = null)
    {
        var settings = new Mock<IEffectiveSettingsProvider>();
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxQueuedPipelineTriggers, null, null)).Returns(capacity);
        var context = accessor ?? new Mock<IAuthenticationContextAccessor>();
        context.SetupProperty(a => a.Current);
        return new ServiceCollection().AddLogging().AddSingleton(settings.Object).AddSingleton(context.Object).AddSingleton(Store().Object).AddSingleton(Stops()).BuildServiceProvider();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheQueueHoldsTheNumberItWasGivenAndDropsTheTriggerPastIt()
    {
        var queue = new PipelineExecutionQueue(2, Store().Object, Stops());

        (await queue.Enqueue(Request(), TestContext.Current.CancellationToken)).IsAccepted.ShouldBeTrue();
        (await queue.Enqueue(Request(), TestContext.Current.CancellationToken)).IsAccepted.ShouldBeTrue();
        (await queue.Enqueue(Request(), TestContext.Current.CancellationToken)).IsAccepted.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheRequestIsRecordedBeforeTheQueueTakesItWhateverTheQueueAnswers()
    {
        var store = Store();
        var queue = new PipelineExecutionQueue(1, store.Object, Stops());
        var first = Request();
        var second = Request();

        (await queue.Enqueue(first, TestContext.Current.CancellationToken)).IsAccepted.ShouldBeTrue();
        (await queue.Enqueue(second, TestContext.Current.CancellationToken)).IsAccepted.ShouldBeFalse();

        store.Verify(s => s.Record(first, It.IsAny<CancellationToken>()), Times.Once);
        store.Verify(s => s.Record(second, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARequestThatCannotBeRecordedIsNotTakenAndTheCallerIsToldWhyAndNotThatTheQueueIsFull()
    {
        var queue = new PipelineExecutionQueue(5, Store(records: false).Object, Stops());

        var failure = await Should.ThrowAsync<InvalidOperationException>(async () => await queue.Enqueue(Request(), TestContext.Current.CancellationToken));

        failure.Message.ShouldContain("the record is not available");
        queue.Reader.TryRead(out _).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ACapacityThatIsNotPositiveIsRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new PipelineExecutionQueue(0, Store().Object, Stops()));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheHostBuildsTheQueueFromItsSettingUnderTheSystemContext()
    {
        var accessor = new Mock<IAuthenticationContextAccessor>();
        await using var host = Host(1, accessor);

        var queue = PipelineExecutionQueue.Create(host);

        accessor.Object.Current.ShouldBeAssignableTo<SystemAuthenticationContext>();
        (await queue.Enqueue(Request(), TestContext.Current.CancellationToken)).IsAccepted.ShouldBeTrue();
        (await queue.Enqueue(Request(), TestContext.Current.CancellationToken)).IsAccepted.ShouldBeFalse();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task AHostWhoseSettingIsNotPositiveGetsNoQueueAndTheFailureNamesTheSetting(int capacity)
    {
        await using var host = Host(capacity);

        var failure = Should.Throw<InvalidOperationException>(() => PipelineExecutionQueue.Create(host));

        failure.Message.ShouldContain("MaxQueuedPipelineTriggers");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AFullQueueAnswersWithTheQueueFullCodeAndAReasonNamingTheNumber()
    {
        var queue = new PipelineExecutionQueue(1, Store().Object, Stops());
        await queue.Enqueue(Request(), TestContext.Current.CancellationToken);

        var dropped = await queue.Enqueue(Request(), TestContext.Current.CancellationToken);

        dropped.IsAccepted.ShouldBeFalse();
        dropped.Code.ShouldBe("QueueFull");
        dropped.Reason.ShouldContain("1");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunAThresholdStopsIsNotRecordedNotQueuedAndAnswersServerBusyWithTheThresholdsReason()
    {
        var store = Store();
        var stops = Stops(GenericResult<PipelineRunStopCheck>.Success(new PipelineRunStopCheck(true, "Queue full", "The server is not taking new runs: the threshold 'Queue full' fires")));
        var queue = new PipelineExecutionQueue(5, store.Object, stops);

        var result = await queue.Enqueue(Request(), TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Code.ShouldBe("ServerBusy");
        result.Reason.ShouldContain("Queue full");
        store.Verify(s => s.Record(It.IsAny<PipelineExecutionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AServerWhoseThresholdsCannotBeReadThrowsNamingWhyAndNeverTakesTheRunUnjudged()
    {
        var store = Store();
        var stops = Stops(GenericResult<PipelineRunStopCheck>.Failure(new GenericMessage("the pipeline 'Monthly' has no level of thresholds")));
        var queue = new PipelineExecutionQueue(5, store.Object, stops);

        var failure = await Should.ThrowAsync<InvalidOperationException>(async () => await queue.Enqueue(Request(), TestContext.Current.CancellationToken));

        failure.Message.ShouldContain("Monthly");
        store.Verify(s => s.Record(It.IsAny<PipelineExecutionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
