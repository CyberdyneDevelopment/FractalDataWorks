using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.DevSession.Logging;

/// <summary>
/// MessageLogging for the git-backed worktree engine.
/// EventId range: 12010-12029 (trace/info), 92010-92029 (errors).
/// </summary>
[MessageLoggingTypeCode("DEVSESSION")]
public static partial class WorktreeEngineLog
{
    [MessageLogging(
        EventId = 12010,
        Level = LogLevel.Trace,
        Message = "Running git {arguments} in {workingDirectory}")]
    public static partial IGenericMessage GitInvoking(
        ILogger logger,
        string arguments,
        string workingDirectory);

    [MessageLogging(
        EventId = 12011,
        Level = LogLevel.Trace,
        Message = "git {arguments} exited {exitCode}")]
    public static partial IGenericMessage GitCompleted(
        ILogger logger,
        string arguments,
        int exitCode);

    [MessageLogging(
        EventId = 12012,
        Level = LogLevel.Information,
        Message = "Created branch {branchName} from {baseRef} in {repoPath}")]
    public static partial IGenericMessage BranchCreated(
        ILogger logger,
        string branchName,
        string baseRef,
        string repoPath);

    [MessageLogging(
        EventId = 12013,
        Level = LogLevel.Information,
        Message = "Created worktree {worktreePath} on branch {branchName} from {baseRef}")]
    public static partial IGenericMessage WorktreeCreated(
        ILogger logger,
        string worktreePath,
        string branchName,
        string baseRef);

    [MessageLogging(
        EventId = 12014,
        Level = LogLevel.Information,
        Message = "Committed {commitSha} in {path}")]
    public static partial IGenericMessage Committed(
        ILogger logger,
        string commitSha,
        string path);

    [MessageLogging(
        EventId = 12015,
        Level = LogLevel.Information,
        Message = "Pushed {branchName} to {remote}")]
    public static partial IGenericMessage Pushed(
        ILogger logger,
        string branchName,
        string remote);

    [MessageLogging(
        EventId = 12016,
        Level = LogLevel.Information,
        Message = "Merged {sourceBranch} into {targetBranch} in {repoPath}")]
    public static partial IGenericMessage Merged(
        ILogger logger,
        string sourceBranch,
        string targetBranch,
        string repoPath);

    [MessageLogging(
        EventId = 12017,
        Level = LogLevel.Information,
        Message = "Removed worktree {worktreePath}")]
    public static partial IGenericMessage WorktreeRemoved(
        ILogger logger,
        string worktreePath);

    [MessageLogging(
        EventId = 12018,
        Level = LogLevel.Trace,
        Message = "Nothing to commit in {path}")]
    public static partial IGenericMessage NothingToCommit(
        ILogger logger,
        string path);

    [MessageLogging(
        EventId = 92010,
        Level = LogLevel.Error,
        Message = "git {arguments} failed with exit code {exitCode}: {error}")]
    public static partial IGenericMessage GitFailed(
        ILogger logger,
        string arguments,
        int exitCode,
        string error);

    [MessageLogging(
        EventId = 92011,
        Level = LogLevel.Error,
        Message = "git executable could not be started: {error}")]
    public static partial IGenericMessage GitUnavailable(
        ILogger logger,
        string error);

    [MessageLogging(
        EventId = 92012,
        Level = LogLevel.Error,
        Message = "Repository path does not exist or is not a git repository: {repoPath}")]
    public static partial IGenericMessage RepoPathInvalid(
        ILogger logger,
        string repoPath);

    [MessageLogging(
        EventId = 92013,
        Level = LogLevel.Error,
        Message = "Worktree path is required to create a worktree but was not supplied for branch {branchName}")]
    public static partial IGenericMessage WorktreePathMissing(
        ILogger logger,
        string branchName);

    [MessageLogging(
        EventId = 92014,
        Level = LogLevel.Error,
        Message = "Isolated copy has no worktree path, so it cannot be removed as a worktree: branch {branchName}")]
    public static partial IGenericMessage NotAWorktree(
        ILogger logger,
        string branchName);
}
