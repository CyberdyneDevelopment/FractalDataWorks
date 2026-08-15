using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.DevSession.Abstractions;
using Fdw.DevSession.Git;
using Fdw.DevSession.Sessions;

namespace Fdw.DevSession.Tests;

/// <summary>Behaviour of <see cref="WorkspaceCoordinator"/> — strand concurrency within one session.</summary>
public sealed class WorkspaceCoordinatorTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private sealed record Harness(
        DevSessionManager Sessions,
        WorkspaceCoordinator Coordinator,
        RecordingEventBus Bus,
        TemporaryRepository Repository) : IDisposable
    {
        public void Dispose() => Repository.Dispose();
    }

    private static Harness CreateHarness()
    {
        var repository = TemporaryRepository.CreateWithInitialCommit();
        var bus = new RecordingEventBus();
        var sessions = new DevSessionManager(new GitWorktreeEngine(new GitProcessRunner()), bus);
        return new Harness(sessions, new WorkspaceCoordinator(sessions, bus), bus, repository);
    }

    private static async Task<Guid> OpenSession(Harness harness, string key, string branch)
    {
        var opened = await harness.Sessions.Open(
            new SessionRequest(key, new IsolationRequest(harness.Repository.Path, "main", branch), "Branch"),
            Token);
        opened.IsSuccess.ShouldBeTrue(opened.CurrentMessage);
        return opened.Value!.Id;
    }

    [Fact]
    public async Task FenceStrand_grants_a_claim_over_the_requested_paths()
    {
        using var harness = CreateHarness();
        var sessionId = await OpenSession(harness, "FDW-20", "feature/twenty");

        var result = await harness.Coordinator.FenceStrand(
            sessionId, new ScopeRequest("strand-a", ["src/Foo.cs"]), Token);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.StrandId.ShouldBe("strand-a");
        result.Value!.SessionId.ShouldBe(sessionId);
    }

    [Fact]
    public async Task FenceStrand_refuses_a_claim_that_overlaps_a_live_strand()
    {
        using var harness = CreateHarness();
        var sessionId = await OpenSession(harness, "FDW-21", "feature/twentyone");
        await harness.Coordinator.FenceStrand(sessionId, new ScopeRequest("strand-a", ["src"]), Token);

        var result = await harness.Coordinator.FenceStrand(
            sessionId, new ScopeRequest("strand-b", ["src/Nested/Thing.cs"]), Token);

        // Why: "src" is an ancestor of the requested path, so the two strands would write the same
        // subtree. The claim is refused rather than narrowed.
        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("strand-a");
    }

    [Fact]
    public async Task FenceStrand_allows_non_overlapping_strands_to_run_concurrently()
    {
        using var harness = CreateHarness();
        var sessionId = await OpenSession(harness, "FDW-22", "feature/twentytwo");
        await harness.Coordinator.FenceStrand(sessionId, new ScopeRequest("strand-a", ["src/Foo.cs"]), Token);

        var result = await harness.Coordinator.FenceStrand(
            sessionId, new ScopeRequest("strand-b", ["src/Bar.cs"]), Token);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        (await harness.Coordinator.ListStrands(sessionId, Token)).Value!.Count.ShouldBe(2);
    }

    [Fact]
    public async Task FenceStrand_refuses_a_duplicate_strand_id()
    {
        using var harness = CreateHarness();
        var sessionId = await OpenSession(harness, "FDW-23", "feature/twentythree");
        await harness.Coordinator.FenceStrand(sessionId, new ScopeRequest("strand-a", ["src/Foo.cs"]), Token);

        var result = await harness.Coordinator.FenceStrand(
            sessionId, new ScopeRequest("strand-a", ["docs/other.md"]), Token);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("already fenced");
    }

    [Fact]
    public async Task FenceStrand_refuses_an_empty_scope()
    {
        using var harness = CreateHarness();
        var sessionId = await OpenSession(harness, "FDW-24", "feature/twentyfour");

        var result = await harness.Coordinator.FenceStrand(sessionId, new ScopeRequest("strand-a", []), Token);

        // Why: an empty claim fences nothing while reading as granted, so every later overlap check
        // would pass and the strand would look safely scoped when it is not.
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task FenceStrand_fails_loud_for_an_unknown_session()
    {
        using var harness = CreateHarness();

        var result = await harness.Coordinator.FenceStrand(
            Guid.NewGuid(), new ScopeRequest("strand-a", ["src"]), Token);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Strands_in_different_sessions_do_not_contend()
    {
        using var harness = CreateHarness();
        var first = await OpenSession(harness, "FDW-25", "feature/twentyfive");
        var second = await OpenSession(harness, "FDW-26", "feature/twentysix");
        await harness.Coordinator.FenceStrand(first, new ScopeRequest("strand-a", ["src/Foo.cs"]), Token);

        var result = await harness.Coordinator.FenceStrand(second, new ScopeRequest("strand-a", ["src/Foo.cs"]), Token);

        // Why: separate sessions have separate isolated copies, so the same relative path is a
        // different file in each.
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }

    [Fact]
    public async Task Reconcile_releases_the_claim_so_the_paths_can_be_refenced()
    {
        using var harness = CreateHarness();
        var sessionId = await OpenSession(harness, "FDW-27", "feature/twentyseven");
        await harness.Coordinator.FenceStrand(sessionId, new ScopeRequest("strand-a", ["src/Foo.cs"]), Token);

        var reconciled = await harness.Coordinator.Reconcile(sessionId, "strand-a", Token);
        reconciled.IsSuccess.ShouldBeTrue(reconciled.CurrentMessage);
        reconciled.Value!.State.Name.ShouldBe("Reconciled");
        reconciled.Value!.State.IsTerminal.ShouldBeTrue();

        var refenced = await harness.Coordinator.FenceStrand(
            sessionId, new ScopeRequest("strand-b", ["src/Foo.cs"]), Token);
        refenced.IsSuccess.ShouldBeTrue(refenced.CurrentMessage);
    }

    [Fact]
    public async Task Reconcile_fails_loud_for_an_already_terminal_strand()
    {
        using var harness = CreateHarness();
        var sessionId = await OpenSession(harness, "FDW-28", "feature/twentyeight");
        await harness.Coordinator.FenceStrand(sessionId, new ScopeRequest("strand-a", ["src/Foo.cs"]), Token);
        await harness.Coordinator.Reconcile(sessionId, "strand-a", Token);

        var result = await harness.Coordinator.Reconcile(sessionId, "strand-a", Token);

        // Why: reconciling twice would re-release a claim another strand may already hold.
        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("terminal");
    }

    [Fact]
    public async Task Reconcile_fails_loud_for_an_unknown_strand()
    {
        using var harness = CreateHarness();
        var sessionId = await OpenSession(harness, "FDW-29", "feature/twentynine");

        var result = await harness.Coordinator.Reconcile(sessionId, "never-fenced", Token);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Route_fails_loud_when_no_handler_is_registered()
    {
        using var harness = CreateHarness();
        var sessionId = await OpenSession(harness, "FDW-30", "feature/thirty");
        var claim = (await harness.Coordinator.FenceStrand(
            sessionId, new ScopeRequest("strand-a", ["src/Foo.cs"]), Token)).Value!;
        var strands = (await harness.Coordinator.ListStrands(sessionId, Token)).Value!;

        var result = await harness.Coordinator.Route(sessionId, strands.Single(), Token);

        // Why: StrandHandlers ships EMPTY on purpose — the framework owns routing, handlers are
        // consumer domain work. An unroutable strand is a real configuration gap, reported as one.
        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("strand-a");
        claim.Paths.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Lifecycle_events_reach_the_bus_as_the_ledger()
    {
        using var harness = CreateHarness();
        var sessionId = await OpenSession(harness, "FDW-31", "feature/thirtyone");

        await harness.Coordinator.FenceStrand(sessionId, new ScopeRequest("strand-a", ["src/Foo.cs"]), Token);
        await harness.Coordinator.Reconcile(sessionId, "strand-a", Token);

        var topics = harness.Bus.Published.Select(e => e.Topic).ToArray();
        topics.ShouldContain(DevSessionTopics.For(sessionId, DevSessionTopics.StrandFenced));
        topics.ShouldContain(DevSessionTopics.For(sessionId, DevSessionTopics.StrandReconciled));
    }

    [Fact]
    public async Task ListStrands_is_empty_for_a_session_with_no_strands()
    {
        using var harness = CreateHarness();
        var sessionId = await OpenSession(harness, "FDW-32", "feature/thirtytwo");

        var result = await harness.Coordinator.ListStrands(sessionId, Token);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.ShouldBeEmpty();
    }
}
