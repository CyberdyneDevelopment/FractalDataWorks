using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// FDW-6139: a step that acts outside the run is claimed before it acts. A step claimed before does not act again: one that finished is taken as done, one that failed fails again, one that never recorded
/// how it ended stops the run. A step in a loop is claimed once for each round, a restarted run finds the claims of the first run of its chain, and a failure of the effect leaves the claim in place.
/// </summary>
public sealed class FlowClaimedStepTests
{
    private static readonly Guid Tenant = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
    private static readonly Guid FirstRun = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid SecondRun = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");

    private sealed class CountingStep(string name, IGenericResult answer) : IFlowStep
    {
        public int Acted { get; private set; }

        public string Name => name;

        public Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default)
        {
            Acted++;
            return Task.FromResult(answer);
        }
    }

    private static FlowStepContext Context(Guid run, Guid? continues = null) =>
        new(new FlowVariables(), new FlowRunTally(new FlowExecutor()), Options(), null, new FlowRunIdentity(run, Tenant, ["etl-runner"]) { ContinuesRun = continues });

    private static FlowClaimedStep Claimed(CountingStep inner, InMemoryStepClaims claims) => new(inner, claims, NullLogger.Instance);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ANewClaimLetsTheStepActAndRecordsHowItEnded()
    {
        var claims = new InMemoryStepClaims();
        var inner = new CountingStep("send", GenericResult.Success());

        var ran = await Claimed(inner, claims).Run(Context(FirstRun), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        inner.Acted.ShouldBe(1);
        claims.OutcomeOf(FirstRun, "send", 0).ShouldBe(StepClaimOutcomes.Succeeded);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStepThatFailedKeepsItsClaimAndFailsAgainWithoutActing()
    {
        var claims = new InMemoryStepClaims();
        var failing = new CountingStep("send", GenericResult.Failure(new GenericMessage("the mail server refused")));
        var first = await Claimed(failing, claims).Run(Context(FirstRun), TestContext.Current.CancellationToken);
        first.IsFailure.ShouldBeTrue();
        claims.OutcomeOf(FirstRun, "send", 0).ShouldBe(StepClaimOutcomes.Failed);

        var restarted = new CountingStep("send", GenericResult.Success());
        var again = await Claimed(restarted, claims).Run(Context(SecondRun, continues: FirstRun), TestContext.Current.CancellationToken);

        again.IsFailure.ShouldBeTrue();
        again.CurrentMessage.ShouldNotBeNull().ShouldContain("failed there");
        restarted.Acted.ShouldBe(0, "a step that failed is not tried a second time");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARestartedRunTakesAFinishedStepAsDoneAndDoesNotActAgain()
    {
        var claims = new InMemoryStepClaims();
        await Claimed(new CountingStep("send", GenericResult.Success()), claims).Run(Context(FirstRun), TestContext.Current.CancellationToken);

        var restarted = new CountingStep("send", GenericResult.Success());
        var again = await Claimed(restarted, claims).Run(Context(SecondRun, continues: FirstRun), TestContext.Current.CancellationToken);

        again.IsSuccess.ShouldBeTrue(again.CurrentMessage);
        restarted.Acted.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStepClaimedThatNeverRecordedHowItEndedStopsTheRunAndDoesNotAct()
    {
        var claims = new InMemoryStepClaims();
        claims.Seed(Tenant, FirstRun, "send", 0, FirstRun, outcome: null);
        var restarted = new CountingStep("send", GenericResult.Success());

        var again = await Claimed(restarted, claims).Run(Context(SecondRun, continues: FirstRun), TestContext.Current.CancellationToken);

        again.IsFailure.ShouldBeTrue();
        again.CurrentMessage.ShouldNotBeNull().ShouldContain("never recorded how it ended");
        restarted.Acted.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStepInALoopIsClaimedOnceForEachRound()
    {
        var claims = new InMemoryStepClaims();
        var inner = new CountingStep("send", GenericResult.Success());
        var step = Claimed(inner, claims);
        var context = Context(FirstRun);

        await step.Run(context, TestContext.Current.CancellationToken);
        await step.Run(context, TestContext.Current.CancellationToken);

        inner.Acted.ShouldBe(2);
        claims.Made.Select(claim => claim.Iteration).ShouldBe([0, 1]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARestartedLoopTakesTheRoundsTheFirstRunFinishedAndActsOnTheNextOne()
    {
        var claims = new InMemoryStepClaims();
        var firstRun = Claimed(new CountingStep("send", GenericResult.Success()), claims);
        await firstRun.Run(Context(FirstRun), TestContext.Current.CancellationToken);

        var restarted = new CountingStep("send", GenericResult.Success());
        var step = Claimed(restarted, claims);
        await step.Run(Context(SecondRun, continues: FirstRun), TestContext.Current.CancellationToken);
        await step.Run(Context(SecondRun, continues: FirstRun), TestContext.Current.CancellationToken);

        restarted.Acted.ShouldBe(1, "round 0 was finished by the first run; round 1 is new");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnOutcomeThatCannotBeRecordedFailsTheStepEvenThoughItActed()
    {
        var claims = new InMemoryStepClaims { RecordFails = true };
        var inner = new CountingStep("send", GenericResult.Success());

        var ran = await Claimed(inner, claims).Run(Context(FirstRun), TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        inner.Acted.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunWithNoIdentityHasNothingToClaimUnderAndTheStepDoesNotAct()
    {
        var inner = new CountingStep("send", GenericResult.Success());
        var untracked = new FlowStepContext(new FlowVariables(), new FlowRunTally(new FlowExecutor()), Options(), null, null);

        var ran = await Claimed(inner, new InMemoryStepClaims()).Run(untracked, TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        inner.Acted.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheRootOfAChainIsTheFirstRunAndARunThatIsNotARestartIsItsOwnRoot()
    {
        new FlowRunIdentity(SecondRun, Tenant, []) { ContinuesRun = FirstRun }.ClaimRootId.ShouldBe(FirstRun);
        new FlowRunIdentity(SecondRun, Tenant, []).ClaimRootId.ShouldBe(SecondRun);
    }
}
