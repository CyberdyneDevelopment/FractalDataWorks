using System.IO;
using System.Threading.Tasks;
using Fdw.DevSession.Abstractions;
using Fdw.DevSession.Git;

namespace Fdw.DevSession.Tests;

/// <summary>Behaviour of <see cref="GitWorktreeEngine"/> against real git repositories.</summary>
public sealed class GitWorktreeEngineTests
{
    private static GitWorktreeEngine CreateEngine() => new(new GitProcessRunner());

    [Fact]
    public async Task CreateBranch_creates_the_branch_at_the_requested_base()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var engine = CreateEngine();

        var result = await engine.CreateBranch(
            new IsolationRequest(repository.Path, "main", "feature/work"));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.BranchName.ShouldBe("feature/work");
        result.Value!.IsolationLevelName.ShouldBe("Branch");
        result.Value!.WorktreePath.ShouldBeNull();
        repository.Git("rev-parse", "--verify", "feature/work").ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CreateBranch_fails_loud_when_the_base_ref_does_not_exist()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var engine = CreateEngine();

        var result = await engine.CreateBranch(
            new IsolationRequest(repository.Path, "no-such-ref", "feature/work"));

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CreateBranch_carries_unpushed_local_commits_into_the_branch()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        repository.WriteFile("local.txt", "committed locally, never pushed");
        repository.Git("add", "-A");
        repository.Git("commit", "-m", "local only");
        var localHead = repository.Git("rev-parse", "HEAD");
        var engine = CreateEngine();

        var result = await engine.CreateBranch(
            new IsolationRequest(repository.Path, "main", "feature/from-local"));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        // Why this test exists: branching from a fetched origin/* ref instead of the caller's local
        // HEAD silently discards unpushed work. That is the specific failure the workspace protocol
        // forbids, so it is pinned here rather than left to review.
        repository.Git("rev-parse", "feature/from-local").ShouldBe(localHead);
    }

    [Fact]
    public async Task CreateWorktree_materializes_a_working_directory_on_a_new_branch()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var worktreePath = Path.Combine(repository.Root, "wt");
        var engine = CreateEngine();

        var result = await engine.CreateWorktree(
            new IsolationRequest(repository.Path, "main", "feature/wt") { WorktreePath = worktreePath });

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.WorktreePath.ShouldBe(worktreePath);
        result.Value!.IsolationLevelName.ShouldBe("Worktree");
        Directory.Exists(worktreePath).ShouldBeTrue();
        File.Exists(Path.Combine(worktreePath, "README.md")).ShouldBeTrue();
    }

    [Fact]
    public async Task CreateWorktree_fails_loud_when_no_worktree_path_is_supplied()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var engine = CreateEngine();

        var result = await engine.CreateWorktree(
            new IsolationRequest(repository.Path, "main", "feature/no-path"));

        // Why: the engine must not invent a filesystem location. Remove() later deletes whatever
        // path is recorded here, so a guessed one is actively dangerous.
        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldContain("worktree path");
    }

    [Fact]
    public async Task Commit_commits_changes_made_inside_the_worktree()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var worktreePath = Path.Combine(repository.Root, "wt");
        var engine = CreateEngine();
        var copy = (await engine.CreateWorktree(
            new IsolationRequest(repository.Path, "main", "feature/commit") { WorktreePath = worktreePath })).Value!;
        File.WriteAllText(Path.Combine(worktreePath, "added.txt"), "new work");

        var result = await engine.Commit(copy, "add work");

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Length.ShouldBe(40);
        repository.GitIn(worktreePath, "log", "-1", "--pretty=%s").ShouldBe("add work");
    }

    [Fact]
    public async Task Commit_fails_loud_when_there_is_nothing_to_commit()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var worktreePath = Path.Combine(repository.Root, "wt");
        var engine = CreateEngine();
        var copy = (await engine.CreateWorktree(
            new IsolationRequest(repository.Path, "main", "feature/empty") { WorktreePath = worktreePath })).Value!;

        var result = await engine.Commit(copy, "nothing changed");

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldContain("Nothing to commit");
    }

    [Fact]
    public async Task Merge_brings_the_source_branch_into_the_target()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var worktreePath = Path.Combine(repository.Root, "wt");
        var engine = CreateEngine();
        var copy = (await engine.CreateWorktree(
            new IsolationRequest(repository.Path, "main", "feature/merge") { WorktreePath = worktreePath })).Value!;
        File.WriteAllText(Path.Combine(worktreePath, "merged.txt"), "content");
        await engine.Commit(copy, "work to merge");

        var result = await engine.Merge(repository.Path, "feature/merge", "main");

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        File.Exists(Path.Combine(repository.Path, "merged.txt")).ShouldBeTrue();
    }

    [Fact]
    public async Task Merge_fails_loud_on_a_conflict_rather_than_resolving_it()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var worktreePath = Path.Combine(repository.Root, "wt");
        var engine = CreateEngine();
        var copy = (await engine.CreateWorktree(
            new IsolationRequest(repository.Path, "main", "feature/conflict") { WorktreePath = worktreePath })).Value!;
        File.WriteAllText(Path.Combine(worktreePath, "README.md"), "branch version");
        await engine.Commit(copy, "branch edit");
        repository.WriteFile("README.md", "main version");
        repository.Git("add", "-A");
        repository.Git("commit", "-m", "main edit");

        var result = await engine.Merge(repository.Path, "feature/conflict", "main");

        // Why: which side wins is never the engine's call, so a conflict is surfaced, not resolved
        // and not auto-aborted.
        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Remove_deletes_the_worktree_directory()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var worktreePath = Path.Combine(repository.Root, "wt");
        var engine = CreateEngine();
        var copy = (await engine.CreateWorktree(
            new IsolationRequest(repository.Path, "main", "feature/remove") { WorktreePath = worktreePath })).Value!;

        var result = await engine.Remove(copy);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldBeTrue();
        Directory.Exists(worktreePath).ShouldBeFalse();
        repository.Git("worktree", "list").ShouldNotContain(worktreePath);
    }

    [Fact]
    public async Task Remove_fails_loud_for_a_branch_only_copy()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var engine = CreateEngine();
        var copy = (await engine.CreateBranch(
            new IsolationRequest(repository.Path, "main", "feature/branch-only"))).Value!;

        var result = await engine.Remove(copy);

        // Why: Remove deletes a working directory. Being handed a copy that never had one means the
        // caller confused two isolation levels; succeeding quietly would hide that.
        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldContain("no worktree path");
    }

    [Fact]
    public async Task Run_fails_loud_when_the_working_directory_does_not_exist()
    {
        var engine = CreateEngine();

        var result = await engine.CreateBranch(
            new IsolationRequest(Path.Combine(Path.GetTempPath(), "fdw-does-not-exist-" + Path.GetRandomFileName()), "main", "b"));

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task WorktreeIsolation_materializes_through_the_engine()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var worktreePath = Path.Combine(repository.Root, "wt");
        var engine = CreateEngine();

        // Why: exercises the TypeOption's behavioural Materialize rather than calling the engine
        // directly, so the registered isolation level and the engine stay in agreement.
        var result = await new WorktreeIsolation().Materialize(
            engine,
            new IsolationRequest(repository.Path, "main", "feature/via-option") { WorktreePath = worktreePath });

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        Directory.Exists(worktreePath).ShouldBeTrue();
    }
}
