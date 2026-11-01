using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.DevSession.Abstractions;
using Fdw.DevSession.Logging;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.DevSession.Git;

/// <summary>Git-backed implementation of <see cref="IWorktreeEngine"/>.</summary>
/// <remarks>
/// <para>
/// Every operation bases work on the ref the caller names and never substitutes one. In particular
/// nothing here fetches, and nothing rewrites a caller's base ref to an <c>origin/*</c> equivalent:
/// the workspace protocol branches from the main checkout's LOCAL HEAD precisely so that local
/// commits that have not been pushed are carried into the isolated copy, and silently preferring a
/// remote ref would drop that work.
/// </para>
/// <para>
/// A git command that exits non-zero is always surfaced as a failure carrying git's own stderr. The
/// engine never infers "already done" from an error and never continues past one.
/// </para>
/// </remarks>
public sealed class GitWorktreeEngine : IWorktreeEngine
{
    private readonly IGitRunner _runner;
    private readonly ILogger<GitWorktreeEngine> _logger;

    /// <summary>Initializes the engine.</summary>
    public GitWorktreeEngine(IGitRunner runner, ILogger<GitWorktreeEngine>? logger = null)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _logger = logger ?? NullLogger<GitWorktreeEngine>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IsolatedCopy>> CreateBranch(
        IsolationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var created = await Git(
            request.RepoPath,
            cancellationToken,
            "branch", request.BranchName, request.BaseRef).ConfigureAwait(false);
        if (created.IsFailure) return created.ToNewResult<IsolatedCopy>();

        WorktreeEngineLog.BranchCreated(_logger, request.BranchName, request.BaseRef, request.RepoPath);

        return GenericResult<IsolatedCopy>.Success(
            new IsolatedCopy(
                request.RepoPath,
                request.BaseRef,
                request.BranchName,
                BranchIsolationName));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IsolatedCopy>> CreateWorktree(
        IsolationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.WorktreePath))
        {
            return GenericResult<IsolatedCopy>.Failure(
                WorktreeEngineLog.WorktreePathMissing(_logger, request.BranchName));
        }

        var added = await Git(
            request.RepoPath,
            cancellationToken,
            "worktree", "add", request.WorktreePath!, "-b", request.BranchName, request.BaseRef).ConfigureAwait(false);
        if (added.IsFailure) return added.ToNewResult<IsolatedCopy>();

        WorktreeEngineLog.WorktreeCreated(_logger, request.WorktreePath!, request.BranchName, request.BaseRef);

        return GenericResult<IsolatedCopy>.Success(
            new IsolatedCopy(
                request.RepoPath,
                request.BaseRef,
                request.BranchName,
                WorktreeIsolationName)
            {
                WorktreePath = request.WorktreePath,
            });
    }

    /// <inheritdoc />
    public async Task<IGenericResult<string>> Commit(
        IsolatedCopy copy,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (copy is null) throw new ArgumentNullException(nameof(copy));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Commit message is required.", nameof(message));

        var path = WorkingPath(copy);

        var staged = await Git(path, cancellationToken, "add", "-A").ConfigureAwait(false);
        if (staged.IsFailure) return staged.ToNewResult<string>();

        var pending = await RawGit(path, cancellationToken, "diff", "--cached", "--quiet").ConfigureAwait(false);
        if (pending.IsFailure) return pending.ToNewResult<string>();
        if (pending.Value!.ExitCode == 0)
        {
            return GenericResult<string>.Failure(WorktreeEngineLog.NothingToCommit(_logger, path));
        }

        var committed = await Git(path, cancellationToken, "commit", "-m", message).ConfigureAwait(false);
        if (committed.IsFailure) return committed.ToNewResult<string>();

        var sha = await Git(path, cancellationToken, "rev-parse", "HEAD").ConfigureAwait(false);
        if (sha.IsFailure) return sha.ToNewResult<string>();

        WorktreeEngineLog.Committed(_logger, sha.Value!.StandardOutput, path);
        return GenericResult<string>.Success(sha.Value!.StandardOutput);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<string>> Push(
        IsolatedCopy copy,
        string remote,
        CancellationToken cancellationToken = default)
    {
        if (copy is null) throw new ArgumentNullException(nameof(copy));
        if (string.IsNullOrWhiteSpace(remote)) throw new ArgumentException("Remote is required.", nameof(remote));

        var pushed = await Git(
            WorkingPath(copy),
            cancellationToken,
            "push", "-u", remote, copy.BranchName).ConfigureAwait(false);
        if (pushed.IsFailure) return pushed.ToNewResult<string>();

        WorktreeEngineLog.Pushed(_logger, copy.BranchName, remote);
        return GenericResult<string>.Success(copy.BranchName);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<string>> Merge(
        string repoPath,
        string sourceBranch,
        string targetBranch,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(repoPath)) throw new ArgumentException("Repository path is required.", nameof(repoPath));
        if (string.IsNullOrWhiteSpace(sourceBranch)) throw new ArgumentException("Source branch is required.", nameof(sourceBranch));
        if (string.IsNullOrWhiteSpace(targetBranch)) throw new ArgumentException("Target branch is required.", nameof(targetBranch));

        var checkedOut = await Git(repoPath, cancellationToken, "checkout", targetBranch).ConfigureAwait(false);
        if (checkedOut.IsFailure) return checkedOut.ToNewResult<string>();

        var merged = await Git(repoPath, cancellationToken, "merge", sourceBranch, "--no-edit").ConfigureAwait(false);
        if (merged.IsFailure) return merged.ToNewResult<string>();

        var sha = await Git(repoPath, cancellationToken, "rev-parse", "HEAD").ConfigureAwait(false);
        if (sha.IsFailure) return sha.ToNewResult<string>();

        WorktreeEngineLog.Merged(_logger, sourceBranch, targetBranch, repoPath);
        return GenericResult<string>.Success(sha.Value!.StandardOutput);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<bool>> Remove(
        IsolatedCopy copy,
        CancellationToken cancellationToken = default)
    {
        if (copy is null) throw new ArgumentNullException(nameof(copy));

        if (string.IsNullOrWhiteSpace(copy.WorktreePath))
        {
            return GenericResult<bool>.Failure(
                WorktreeEngineLog.NotAWorktree(_logger, copy.BranchName));
        }

        var removed = await Git(
            copy.RepoPath,
            cancellationToken,
            "worktree", "remove", copy.WorktreePath!).ConfigureAwait(false);
        if (removed.IsFailure) return removed.ToNewResult<bool>();

        WorktreeEngineLog.WorktreeRemoved(_logger, copy.WorktreePath!);
        return GenericResult<bool>.Success(true);
    }

    private const string WorktreeIsolationName = "Worktree";
    private const string BranchIsolationName = "Branch";

    private static string WorkingPath(IsolatedCopy copy)
        => string.IsNullOrWhiteSpace(copy.WorktreePath) ? copy.RepoPath : copy.WorktreePath!;

    private async Task<IGenericResult<GitCommandResult>> Git(
        string workingDirectory,
        CancellationToken cancellationToken,
        params string[] arguments)
    {
        var result = await RawGit(workingDirectory, cancellationToken, arguments).ConfigureAwait(false);
        if (result.IsFailure) return result;

        if (!result.Value!.IsSuccess)
        {
            return GenericResult<GitCommandResult>.Failure(
                WorktreeEngineLog.GitFailed(
                    _logger,
                    string.Join(" ", arguments),
                    result.Value!.ExitCode,
                    result.Value!.StandardError));
        }

        return result;
    }

    private Task<IGenericResult<GitCommandResult>> RawGit(
        string workingDirectory,
        CancellationToken cancellationToken,
        params string[] arguments)
        => _runner.Run(workingDirectory, arguments, cancellationToken);
}
