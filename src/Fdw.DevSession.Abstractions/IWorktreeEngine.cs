using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// Local-git engine that materializes and manages isolated working copies for dev sessions — the
/// "spin up an isolated copy → land commits → push → merge → tear down" mechanic. It is the piece
/// mc3-po's source-control adapters do not model (they mutate a remote over REST); implementations
/// own the actual git operations (CLI or LibGit2Sharp) and fail loud on invalid input — there are
/// no silent defaults for repo path, base ref, branch, or worktree location.
/// </summary>
public interface IWorktreeEngine
{
    /// <summary>
    /// Creates an isolated branch off <see cref="IsolationRequest.BaseRef"/> without a separate working tree.
    /// </summary>
    /// <param name="request">The isolation request describing repo, base ref, and branch name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the created <see cref="IsolatedCopy"/>, or a failure.</returns>
    Task<IGenericResult<IsolatedCopy>> CreateBranch(IsolationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an isolated branch and a checked-out working tree at <see cref="IsolationRequest.WorktreePath"/>.
    /// </summary>
    /// <param name="request">The isolation request; its working-tree path must be supplied.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the created <see cref="IsolatedCopy"/>, or a failure.</returns>
    Task<IGenericResult<IsolatedCopy>> CreateWorktree(IsolationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits all pending changes in the isolated copy.
    /// </summary>
    /// <param name="copy">The isolated copy to commit in.</param>
    /// <param name="message">The commit message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the new commit SHA, or a failure.</returns>
    Task<IGenericResult<string>> Commit(IsolatedCopy copy, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pushes the isolated copy's branch to the named remote (works against GitHub or nexus-vcs Smart-HTTP).
    /// </summary>
    /// <param name="copy">The isolated copy whose branch to push.</param>
    /// <param name="remote">The remote name or URL to push to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the pushed remote ref, or a failure.</returns>
    Task<IGenericResult<string>> Push(IsolatedCopy copy, string remote, CancellationToken cancellationToken = default);

    /// <summary>
    /// Merges one branch into another within the repository.
    /// </summary>
    /// <param name="repoPath">The repository path.</param>
    /// <param name="sourceBranch">The branch to merge from.</param>
    /// <param name="targetBranch">The branch to merge into.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the merge commit SHA, or a failure.</returns>
    Task<IGenericResult<string>> Merge(string repoPath, string sourceBranch, string targetBranch, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tears down the isolated copy, removing its working tree. The branch is retained so its history survives.
    /// </summary>
    /// <param name="copy">The isolated copy to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating whether teardown succeeded.</returns>
    Task<IGenericResult<bool>> Remove(IsolatedCopy copy, CancellationToken cancellationToken = default);
}
