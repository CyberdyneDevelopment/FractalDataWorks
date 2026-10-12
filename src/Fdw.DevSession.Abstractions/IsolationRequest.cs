namespace Fdw.DevSession.Abstractions;

/// <summary>
/// Describes an isolated working copy to create for a dev session: which repository, from which
/// base ref, on which branch, and (optionally) at which working-tree path. No value is defaulted —
/// the caller supplies repo, base ref, and branch name explicitly.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class IsolationRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IsolationRequest"/> class.
    /// </summary>
    /// <param name="repoPath">The absolute path to the source repository.</param>
    /// <param name="baseRef">The ref (branch, tag, or SHA) the isolated copy branches from.</param>
    /// <param name="branchName">The name of the isolated branch to create.</param>
    public IsolationRequest(string repoPath, string baseRef, string branchName)
    {
        RepoPath = repoPath;
        BaseRef = baseRef;
        BranchName = branchName;
    }

    /// <summary>Gets the absolute path to the source repository.</summary>
    public string RepoPath { get; }

    /// <summary>Gets the ref (branch, tag, or SHA) the isolated copy branches from.</summary>
    public string BaseRef { get; }

    /// <summary>Gets the name of the isolated branch to create.</summary>
    public string BranchName { get; }

    /// <summary>
    /// Gets the working-tree path for worktree-based isolation. Null for branch-only isolation;
    /// strategies that require a working tree fail loud when it is absent.
    /// </summary>
    public string? WorktreePath { get; init; }
}
