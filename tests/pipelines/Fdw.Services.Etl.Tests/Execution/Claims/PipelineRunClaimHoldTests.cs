using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Etl.Execution.Claims;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution.Claims;

/// <summary>
/// FDW-6939 (slice 3 of FDW-6141): the holder of a run's durable claim. It claims, asks again at the poll setting while the run waits, renews the lease at the renewal setting while the
/// run runs, fences the run when the lease is lost, and releases the claim when it is disposed. A run that claims no key is claimed at once and holds nothing to renew or release.
/// </summary>
public sealed class PipelineRunClaimHoldTests
{
    private static readonly Guid Tenant = Guid.Parse("22222222-0000-0000-0000-000000000002");
    private static readonly Guid Execution = Guid.Parse("33333333-0000-0000-0000-000000000003");
    private static readonly Guid Pipeline = Guid.Parse("44444444-0000-0000-0000-000000000004");
    private static readonly PipelineRunClaimSettings Settings = new(LeaseSeconds: 60, RenewalSeconds: 20, WaitingPollSeconds: 5, LockTimeoutMilliseconds: 30000);

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long _ticks;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _ticks;

        public void Advance(TimeSpan by) => _ticks += by.Ticks;
    }

    /// <summary>A service that answers what the test queued, and counts what it was asked.</summary>
    private sealed class ScriptedClaims : IPipelineRunClaimService
    {
        private readonly Queue<IGenericResult<PipelineRunClaimOutcome>> _claims = new();
        public Func<IGenericResult<PipelineRunRenewed>> OnRenew { get; set; } = () => GenericResult<PipelineRunRenewed>.Success(new PipelineRunRenewed([new PipelineRunRenewal(Execution, true)], []));
        public int ClaimCalls { get; private set; }
        public int RenewCalls { get; private set; }
        public int ReleaseCalls { get; private set; }
        public SemaphoreSlim Renewed { get; } = new(0);

        public void Answer(string state) =>
            _claims.Enqueue(GenericResult<PipelineRunClaimOutcome>.Success(new PipelineRunClaimOutcome(Execution, PipelineRunClaimStates.ByName(state), state == "Waiting" ? 1 : 0, [])));

        public void AnswerFailure() => _claims.Enqueue(GenericResult<PipelineRunClaimOutcome>.Failure(new GenericMessage("the claim procedure failed")));

        public Task<IGenericResult<PipelineRunClaimOutcome>> Claim(PipelineRunClaimRequest request, CancellationToken cancellationToken = default)
        {
            ClaimCalls++;
            return Task.FromResult(_claims.Dequeue());
        }

        public Task<IGenericResult<PipelineRunRenewed>> Renew(IReadOnlyList<Guid> executionIds, string executingHost, string? executingSlot, CancellationToken cancellationToken = default)
        {
            RenewCalls++;
            var answer = OnRenew();
            _ = Renewed.Release();
            return Task.FromResult(answer);
        }

        public Task<IGenericResult<PipelineRunReleased>> Release(Guid executionId, CancellationToken cancellationToken = default)
        {
            ReleaseCalls++;
            return Task.FromResult<IGenericResult<PipelineRunReleased>>(GenericResult<PipelineRunReleased>.Success(new PipelineRunReleased([], [])));
        }
    }

    /// <summary>A wait the test lets pass one tick at a time, recording how long each was asked to be.</summary>
    private sealed class Ticks : IDisposable
    {
        private readonly SemaphoreSlim _go = new(0);
        public List<TimeSpan> Asked { get; } = [];

        public void Pass() => _go.Release();

        public void Dispose() => _go.Dispose();

        public async Task Wait(TimeSpan span, CancellationToken cancellationToken)
        {
            Asked.Add(span);
            await _go.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static PipelineRunClaimRequest Request(params PipelineRunClaimKey[] keys) =>
        new(Tenant, Execution, Pipeline, keys, "Reference.Api", "b");

    private static PipelineRunClaimKey Write() => PipelineRunClaimKeys.WriteOf("c1/etl/Orders", NullLogger.Instance).Value.ShouldNotBeNull();

    private static Task<IGenericResult<PipelineRunClaimHold>> Acquire(ScriptedClaims claims, PipelineRunClaimRequest request, Ticks ticks, ManualTimeProvider? clock = null, CancellationToken cancellationToken = default) =>
        PipelineRunClaimHold.Acquire(claims, request, Settings, clock ?? new ManualTimeProvider(), ticks.Wait, NullLogger.Instance, cancellationToken);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunThatClaimsNoKeyIsClaimedAtOnceAndHoldsNothingToRenewOrRelease()
    {
        var claims = new ScriptedClaims();
        claims.Answer("Claimed");
        using var ticks = new Ticks();

        var acquired = await Acquire(claims, Request(), ticks, cancellationToken: TestContext.Current.CancellationToken);
        acquired.IsSuccess.ShouldBeTrue(acquired.CurrentMessage);
        await acquired.Value.ShouldNotBeNull().DisposeAsync();

        claims.RenewCalls.ShouldBe(0);
        claims.ReleaseCalls.ShouldBe(0);
        ticks.Asked.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunThatIsClaimedRenewsAtTheRenewalSettingWhileItRunsAndReleasesWhenDisposed()
    {
        var claims = new ScriptedClaims();
        claims.Answer("Claimed");
        using var ticks = new Ticks();

        var acquired = await Acquire(claims, Request(Write()), ticks, cancellationToken: TestContext.Current.CancellationToken);
        var hold = acquired.Value.ShouldNotBeNull();
        ticks.Pass();
        (await claims.Renewed.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken)).ShouldBeTrue();
        await hold.DisposeAsync();

        claims.ClaimCalls.ShouldBe(1);
        claims.RenewCalls.ShouldBeGreaterThanOrEqualTo(1);
        ticks.Asked.ShouldContain(TimeSpan.FromSeconds(20));
        claims.ReleaseCalls.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunThatWaitsAsksAgainAtThePollSettingUntilItIsClaimed()
    {
        var claims = new ScriptedClaims();
        claims.Answer("Waiting");
        claims.Answer("Waiting");
        claims.Answer("Claimed");
        using var ticks = new Ticks();
        ticks.Pass();
        ticks.Pass();

        var acquired = await Acquire(claims, Request(Write()), ticks, cancellationToken: TestContext.Current.CancellationToken);
        await acquired.Value.ShouldNotBeNull().DisposeAsync();

        acquired.IsSuccess.ShouldBeTrue(acquired.CurrentMessage);
        claims.ClaimCalls.ShouldBe(3);
        ticks.Asked.Take(2).ShouldAllBe(span => span == TimeSpan.FromSeconds(5));
        claims.ReleaseCalls.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AClaimThatFailsIsAFailureHoldingNothingAndReleasingNothing()
    {
        var claims = new ScriptedClaims();
        claims.AnswerFailure();

        using var ticks = new Ticks();
        var acquired = await Acquire(claims, Request(Write()), ticks, cancellationToken: TestContext.Current.CancellationToken);

        acquired.IsFailure.ShouldBeTrue();
        claims.ReleaseCalls.ShouldBe(0);
        claims.RenewCalls.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunCancelledWhileItWaitsReleasesItsWaitingRowsAndTheCancellationReachesTheCaller()
    {
        var claims = new ScriptedClaims();
        claims.Answer("Waiting");
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var ticks = new Ticks();

        var acquiring = Acquire(claims, Request(Write()), ticks, cancellationToken: cancel.Token);
        await cancel.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(async () => await acquiring);
        claims.ReleaseCalls.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARenewalThatSaysTheRunHoldsNoRowFencesTheRunWithTheLeaseCode()
    {
        var claims = new ScriptedClaims { OnRenew = () => GenericResult<PipelineRunRenewed>.Success(new PipelineRunRenewed([new PipelineRunRenewal(Execution, false)], [])) };
        claims.Answer("Claimed");
        using var ticks = new Ticks();

        var acquired = await Acquire(claims, Request(Write()), ticks, cancellationToken: TestContext.Current.CancellationToken);
        var hold = acquired.Value.ShouldNotBeNull();
        hold.Check(NullLogger.Instance).IsSuccess.ShouldBeTrue();
        ticks.Pass();
        (await claims.Renewed.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken)).ShouldBeTrue();
        await hold.DisposeAsync();

        hold.Check(NullLogger.Instance).Messages.Select(message => message.Code).ShouldContain("PIPELINERUNCLAIM-60868");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ALeasePastItsDurationWithNoRenewalFencesTheRunAndARenewalThatFailedDoesNotEndTheHold()
    {
        var claims = new ScriptedClaims { OnRenew = () => GenericResult<PipelineRunRenewed>.Failure(new GenericMessage("the renewal procedure failed")) };
        claims.Answer("Claimed");
        using var ticks = new Ticks();
        var clock = new ManualTimeProvider();

        var acquired = await Acquire(claims, Request(Write()), ticks, clock, TestContext.Current.CancellationToken);
        var hold = acquired.Value.ShouldNotBeNull();
        ticks.Pass();
        (await claims.Renewed.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken)).ShouldBeTrue();
        hold.Check(NullLogger.Instance).IsSuccess.ShouldBeTrue("a failed renewal alone does not end the hold: the lease still holds");
        clock.Advance(TimeSpan.FromSeconds(61));
        await hold.DisposeAsync();

        hold.Check(NullLogger.Instance).Messages.Select(message => message.Code).ShouldContain("PIPELINERUNCLAIM-60868");
    }

    // FDW-6939: a run that waits for a pipeline it started hands its claim back while it waits (the same moment it hands its scheduler place back), and takes it again, in the order rule,
    // before it goes on. Otherwise a child whose claim conflicts with its parent's waits on the parent while the parent waits on the child.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunThatYieldsItsClaimReleasesItWhileItWaitsAndClaimsItAgainWhenTheHandleIsDisposed()
    {
        var claims = new ScriptedClaims();
        claims.Answer("Claimed");
        claims.Answer("Waiting");
        claims.Answer("Claimed");
        using var ticks = new Ticks();

        var acquired = await Acquire(claims, Request(Write()), ticks, cancellationToken: TestContext.Current.CancellationToken);
        var hold = acquired.Value.ShouldNotBeNull();

        var yielded = await hold.Yield(TestContext.Current.CancellationToken);
        claims.ReleaseCalls.ShouldBe(1);

        // The renewal is stopped while the claim is handed back, so the one tick that passes is the poll of the run asking for its claim again.
        ticks.Pass();
        await yielded.DisposeAsync();
        claims.ClaimCalls.ShouldBe(3);
        hold.Check(NullLogger.Instance).IsSuccess.ShouldBeTrue();

        await hold.DisposeAsync();
        claims.ReleaseCalls.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunThatClaimsNoKeyHasNothingToYield()
    {
        var claims = new ScriptedClaims();
        claims.Answer("Claimed");

        using var ticks = new Ticks();
        var acquired = await Acquire(claims, Request(), ticks, cancellationToken: TestContext.Current.CancellationToken);
        var yielded = await acquired.Value.ShouldNotBeNull().Yield(TestContext.Current.CancellationToken);
        await yielded.DisposeAsync();

        claims.ReleaseCalls.ShouldBe(0);
        claims.ClaimCalls.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TheClaimOfTheCurrentRunIsYieldedThroughTheAmbientHoldAndAFlowWithNoneYieldsNothing()
    {
        var none = await PipelineRunClaimHold.YieldCurrent(TestContext.Current.CancellationToken);
        await none.DisposeAsync();

        var claims = new ScriptedClaims();
        claims.Answer("Claimed");
        claims.Answer("Claimed");
        using var ticks = new Ticks();
        var acquired = await Acquire(claims, Request(Write()), ticks, cancellationToken: TestContext.Current.CancellationToken);
        var hold = acquired.Value.ShouldNotBeNull();

        PipelineRunClaimHold.Current = hold;
        var yielded = await PipelineRunClaimHold.YieldCurrent(TestContext.Current.CancellationToken);
        claims.ReleaseCalls.ShouldBe(1);
        await yielded.DisposeAsync();
        claims.ClaimCalls.ShouldBe(2);

        PipelineRunClaimHold.Current = null;
        await hold.DisposeAsync();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunCancelledWhileItClaimsAgainAfterAYieldEndsAsCancelledReleasesItsWaitingRowsAndIsFenced()
    {
        var claims = new ScriptedClaims();
        claims.Answer("Claimed");
        claims.Answer("Waiting");
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var ticks = new Ticks();

        var acquired = await Acquire(claims, Request(Write()), ticks, cancellationToken: TestContext.Current.CancellationToken);
        var hold = acquired.Value.ShouldNotBeNull();
        var yielded = await hold.Yield(cancel.Token);
        await cancel.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(async () => await yielded.DisposeAsync());

        claims.ReleaseCalls.ShouldBe(2);
        hold.Check(NullLogger.Instance).Messages.Select(message => message.Code).ShouldContain("PIPELINERUNCLAIM-60868");
    }
}
