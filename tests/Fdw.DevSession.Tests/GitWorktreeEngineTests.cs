using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fdw.DevSession.Abstractions;
using Fdw.DevSession.Git;

namespace Fdw.DevSession.Tests;

/// <summary>Behaviour of <see cref="GitWorktreeEngine"/> against real git repositories.</summary>
public sealed class GitWorktreeEngineTests
{
    private static GitWorktreeEngine CreateEngine() => new(new GitProcessRunner());

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CreateBranch_creates_the_branch_at_the_requested_base()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var engine = CreateEngine();

        var result = await engine.CreateBranch(
            new IsolationRequest(repository.Path, "main", "feature/work"),
            Token);

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
            new IsolationRequest(repository.Path, "no-such-ref", "feature/work"),
            Token);

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
            new IsolationRequest(repository.Path, "main", "feature/from-local"),
            Token);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        repository.Git("rev-parse", "feature/from-local").ShouldBe(localHead);
    }

    [Fact]
    public async Task CreateWorktree_materializes_a_working_directory_on_a_new_branch()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var worktreePath = Path.Combine(repository.Root, "wt");
        var engine = CreateEngine();

        var result = await engine.CreateWorktree(
            new IsolationRequest(repository.Path, "main", "feature/wt") { WorktreePath = worktreePath },
            Token);

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
            new IsolationRequest(repository.Path, "main", "feature/no-path"),
            Token);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("worktree path");
    }

    [Fact]
    public async Task Commit_commits_changes_made_inside_the_worktree()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var worktreePath = Path.Combine(repository.Root, "wt");
        var engine = CreateEngine();
        var created = await engine.CreateWorktree(
            new IsolationRequest(repository.Path, "main", "feature/commit") { WorktreePath = worktreePath },
            Token);
        File.WriteAllText(Path.Combine(worktreePath, "added.txt"), "new work");

        var result = await engine.Commit(created.Value!, "add work", Token);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Length.ShouldBe(40);
        TemporaryRepository.GitIn(worktreePath, "log", "-1", "--pretty=%s").ShouldBe("add work");
    }

    [Fact]
    public async Task Commit_fails_loud_when_there_is_nothing_to_commit()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var worktreePath = Path.Combine(repository.Root, "wt");
        var engine = CreateEngine();
        var created = await engine.CreateWorktree(
            new IsolationRequest(repository.Path, "main", "feature/empty") { WorktreePath = worktreePath },
            Token);

        var result = await engine.Commit(created.Value!, "nothing changed", Token);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Nothing to commit");
    }

    [Fact]
    public async Task Merge_brings_the_source_branch_into_the_target()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var worktreePath = Path.Combine(repository.Root, "wt");
        var engine = CreateEngine();
        var created = await engine.CreateWorktree(
            new IsolationRequest(repository.Path, "main", "feature/merge") { WorktreePath = worktreePath },
            Token);
        File.WriteAllText(Path.Combine(worktreePath, "merged.txt"), "content");
        await engine.Commit(created.Value!, "work to merge", Token);

        var result = await engine.Merge(repository.Path, "feature/merge", "main", Token);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        File.Exists(Path.Combine(repository.Path, "merged.txt")).ShouldBeTrue();
    }

    [Fact]
    public async Task Merge_fails_loud_on_a_conflict_rather_than_resolving_it()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var worktreePath = Path.Combine(repository.Root, "wt");
        var engine = CreateEngine();
        var created = await engine.CreateWorktree(
            new IsolationRequest(repository.Path, "main", "feature/conflict") { WorktreePath = worktreePath },
            Token);
        File.WriteAllText(Path.Combine(worktreePath, "README.md"), "branch version");
        await engine.Commit(created.Value!, "branch edit", Token);
        repository.WriteFile("README.md", "main version");
        repository.Git("add", "-A");
        repository.Git("commit", "-m", "main edit");

        var result = await engine.Merge(repository.Path, "feature/conflict", "main", Token);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Remove_deletes_the_worktree_directory()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var worktreePath = Path.Combine(repository.Root, "wt");
        var engine = CreateEngine();
        var created = await engine.CreateWorktree(
            new IsolationRequest(repository.Path, "main", "feature/remove") { WorktreePath = worktreePath },
            Token);

        var result = await engine.Remove(created.Value!, Token);

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
        var created = await engine.CreateBranch(
            new IsolationRequest(repository.Path, "main", "feature/branch-only"),
            Token);

        var result = await engine.Remove(created.Value!, Token);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("no worktree path");
    }

    [Fact]
    public async Task Engine_fails_loud_when_the_repository_path_does_not_exist()
    {
        var engine = CreateEngine();
        var missing = Path.Combine(Path.GetTempPath(), "fdw-does-not-exist-" + Path.GetRandomFileName());

        var result = await engine.CreateBranch(
            new IsolationRequest(missing, "main", "feature/nowhere"),
            Token);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task WorktreeIsolation_materializes_through_the_engine()
    {
        using var repository = TemporaryRepository.CreateWithInitialCommit();
        var worktreePath = Path.Combine(repository.Root, "wt");
        var engine = CreateEngine();

        var result = await new WorktreeIsolation().Materialize(
            engine,
            new IsolationRequest(repository.Path, "main", "feature/via-option") { WorktreePath = worktreePath },
            Token);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        Directory.Exists(worktreePath).ShouldBeTrue();
    }
}
