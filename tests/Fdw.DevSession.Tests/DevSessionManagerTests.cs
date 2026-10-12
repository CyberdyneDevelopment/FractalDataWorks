using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.DevSession.Abstractions;
using Fdw.DevSession.Git;
using Fdw.DevSession.Sessions;

namespace Fdw.DevSession.Tests;

/// <summary>Behaviour of <see cref="DevSessionManager"/> over real git repositories.</summary>
public sealed class DevSessionManagerTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static (DevSessionManager Manager, RecordingEventBus Bus) CreateManager()
    {
        var bus = new RecordingEventBus();
        return (new DevSessionManager(new GitWorktreeEngine(new GitProcessRunner()), bus), bus);
    }

    private static SessionRequest RequestFor(TemporaryRepository repository, string key, string branch, string? worktree = null)
        => new(
            key,
            new IsolationRequest(repository.Path, "main", branch) { WorktreePath = worktree },
            worktree is null ? "Branch" : "Worktree");

    [Fact]
    public async Task Open_materializes_an_isolated_copy_and_records_it_open()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var (manager, _) = CreateManager();

        var result = await manager.Open(RequestFor(repository, "FDW-1", "feature/one"), Token);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Key.ShouldBe("FDW-1");
        result.Value!.State.Name.ShouldBe("Open");
        result.Value!.ParentSessionId.ShouldBeNull();
        repository.Git("rev-parse", "--verify", "feature/one").ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Open_publishes_the_opened_event_to_the_bus_as_the_ledger()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var (manager, bus) = CreateManager();

        var opened = await manager.Open(RequestFor(repository, "FDW-2", "feature/two"), Token);

        bus.Published.Count.ShouldBe(1);
        bus.Published[0].Topic.ShouldBe(DevSessionTopics.For(opened.Value!.Id, DevSessionTopics.Opened));
        bus.Published[0].PayloadType.ShouldBe(nameof(SessionLedgerEntry));
    }

    [Fact]
    public async Task Open_with_an_existing_key_reuses_the_session_instead_of_branching_twice()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var (manager, _) = CreateManager();
        var first = await manager.Open(RequestFor(repository, "FDW-3", "feature/three"), Token);

        var second = await manager.Open(RequestFor(repository, "FDW-3", "feature/three-again"), Token);

        second.IsSuccess.ShouldBeTrue(second.CurrentMessage);
        second.Value!.Id.ShouldBe(first.Value!.Id);
        manager.List().Count.ShouldBe(1);
        repository.Git("branch", "--list", "feature/three-again").ShouldBeEmpty();
    }

    [Fact]
    public async Task Open_fails_loud_for_an_unregistered_isolation_level()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var (manager, _) = CreateManager();

        var result = await manager.Open(
            new SessionRequest("FDW-4", new IsolationRequest(repository.Path, "main", "feature/four"), "NoSuchLevel"),
            Token);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("NoSuchLevel");
    }

    [Fact]
    public async Task Open_surfaces_the_engine_failure_when_the_copy_cannot_be_materialized()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var (manager, bus) = CreateManager();

        var result = await manager.Open(
            new SessionRequest("FDW-5", new IsolationRequest(repository.Path, "no-such-base", "feature/five"), "Branch"),
            Token);

        result.IsFailure.ShouldBeTrue();
        manager.List().ShouldBeEmpty();
        bus.Published.ShouldBeEmpty();
    }

    [Fact]
    public async Task OpenNested_records_the_parent()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var (manager, _) = CreateManager();
        var parent = await manager.Open(RequestFor(repository, "FDW-6", "feature/parent"), Token);

        var child = await manager.OpenNested(parent.Value!.Id, RequestFor(repository, "FDW-6-side", "feature/child"), Token);

        child.IsSuccess.ShouldBeTrue(child.CurrentMessage);
        child.Value!.ParentSessionId.ShouldBe(parent.Value!.Id);
        manager.List().Count.ShouldBe(2);
    }

    [Fact]
    public async Task OpenNested_fails_loud_for_an_unknown_parent_and_creates_no_branch()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var (manager, _) = CreateManager();

        var result = await manager.OpenNested(Guid.NewGuid(), RequestFor(repository, "FDW-7", "feature/orphan"), Token);

        result.IsFailure.ShouldBeTrue();
        repository.Git("branch", "--list", "feature/orphan").ShouldBeEmpty();
    }

    [Fact]
    public async Task Get_by_id_and_by_key_both_resolve_the_session()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var (manager, _) = CreateManager();
        var opened = await manager.Open(RequestFor(repository, "FDW-8", "feature/eight"), Token);

        manager.Get(opened.Value!.Id).IsSuccess.ShouldBeTrue();
        manager.Get("FDW-8").Value!.Id.ShouldBe(opened.Value!.Id);
    }

    [Fact]
    public void Get_fails_loud_when_the_session_does_not_exist()
    {
        var (manager, _) = CreateManager();

        manager.Get(Guid.NewGuid()).IsFailure.ShouldBeTrue();
        manager.Get("never-opened").IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Sleep_then_Wake_round_trips_the_state()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var (manager, bus) = CreateManager();
        var opened = await manager.Open(RequestFor(repository, "FDW-9", "feature/nine"), Token);

        var slept = await manager.Sleep(opened.Value!.Id, Token);
        slept.Value!.State.Name.ShouldBe("Sleeping");
        slept.Value!.State.IsReclaimable.ShouldBeTrue();

        var woke = await manager.Wake(opened.Value!.Id, Token);
        woke.Value!.State.Name.ShouldBe("Open");

        bus.Published.Select(e => e.Topic).ShouldContain(
            DevSessionTopics.For(opened.Value!.Id, DevSessionTopics.Slept));
        bus.Published.Select(e => e.Topic).ShouldContain(
            DevSessionTopics.For(opened.Value!.Id, DevSessionTopics.Woke));
    }

    [Fact]
    public async Task Wake_fails_loud_when_the_session_was_never_asleep()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var (manager, _) = CreateManager();
        var opened = await manager.Open(RequestFor(repository, "FDW-10", "feature/ten"), Token);

        var result = await manager.Wake(opened.Value!.Id, Token);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("woken");
    }

    [Fact]
    public async Task Close_moves_the_session_to_a_terminal_state()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var (manager, _) = CreateManager();
        var opened = await manager.Open(RequestFor(repository, "FDW-11", "feature/eleven"), Token);

        var closed = await manager.Close(opened.Value!.Id, Token);

        closed.Value!.State.Name.ShouldBe("Done");
        closed.Value!.State.IsTerminal.ShouldBeTrue();
    }

    [Fact]
    public async Task A_closed_session_cannot_be_transitioned_again()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var (manager, _) = CreateManager();
        var opened = await manager.Open(RequestFor(repository, "FDW-12", "feature/twelve"), Token);
        await manager.Close(opened.Value!.Id, Token);

        var result = await manager.Sleep(opened.Value!.Id, Token);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("terminal");
    }

    [Fact]
    public async Task A_key_can_be_reused_once_its_session_is_closed()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var (manager, _) = CreateManager();
        var first = await manager.Open(RequestFor(repository, "FDW-13", "feature/thirteen"), Token);
        await manager.Close(first.Value!.Id, Token);

        var second = await manager.Open(RequestFor(repository, "FDW-13", "feature/thirteen-again"), Token);

        second.IsSuccess.ShouldBeTrue(second.CurrentMessage);
        second.Value!.Id.ShouldNotBe(first.Value!.Id);
    }

    [Fact]
    public async Task Open_with_worktree_isolation_materializes_a_working_directory()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var worktreePath = Path.Combine(repository.Root, "session-wt");
        var (manager, _) = CreateManager();

        var result = await manager.Open(
            RequestFor(repository, "FDW-14", "feature/fourteen", worktreePath),
            Token);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Copy.WorktreePath.ShouldBe(worktreePath);
        Directory.Exists(worktreePath).ShouldBeTrue();
    }
}
