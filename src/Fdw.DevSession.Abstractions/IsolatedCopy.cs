namespace Fdw.DevSession.Abstractions;

/// <summary>
/// A materialized isolated working copy: the branch (and optional working tree) an agent or human
/// works in for the life of a dev session, kept apart from the base until it is submitted for
/// review. This is the "spin up a copy of the project" artifact.
/// </summary>
// Why: pure data holder, no logic beyond trivial construction/assignment
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class IsolatedCopy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IsolatedCopy"/> class.
    /// </summary>
    /// <param name="repoPath">The absolute path to the source repository.</param>
    /// <param name="baseRef">The ref the copy was branched from.</param>
    /// <param name="branchName">The isolated branch name.</param>
    /// <param name="isolationLevelName">The name of the <see cref="IIsolationLevel"/> that produced this copy.</param>
    public IsolatedCopy(string repoPath, string baseRef, string branchName, string isolationLevelName)
    {
        RepoPath = repoPath;
        BaseRef = baseRef;
        BranchName = branchName;
        IsolationLevelName = isolationLevelName;
    }

    /// <summary>Gets the absolute path to the source repository.</summary>
    public string RepoPath { get; }

    /// <summary>Gets the ref the copy was branched from.</summary>
    public string BaseRef { get; }

    /// <summary>Gets the isolated branch name.</summary>
    public string BranchName { get; }

    /// <summary>Gets the name of the isolation strategy that produced this copy.</summary>
    public string IsolationLevelName { get; }

    /// <summary>Gets the working-tree path, when the strategy created one. Null for branch-only isolation.</summary>
    public string? WorktreePath { get; init; }
}
