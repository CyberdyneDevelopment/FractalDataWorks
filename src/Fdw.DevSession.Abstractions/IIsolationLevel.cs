using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// A strategy for materializing an isolated working copy of a repository for a dev session.
/// </summary>
/// <remarks>
/// This is an OPEN collection (<c>[MutableTypeCollection]</c>): a consumer can register its own
/// isolation strategy with a <c>[TypeOption(typeof(IsolationLevels), "...")]</c> option and it is
/// discovered across assemblies. The strategy carries its own <see cref="Materialize"/> behavior,
/// so a custom level is fully functional rather than inert — the platform owns the mechanism seam
/// (<see cref="IWorktreeEngine"/>), never the closed set of strategies.
/// </remarks>
public interface IIsolationLevel : ITypeOption<int, IsolationLevelBase>
{
    /// <summary>
    /// Gets a value indicating whether this strategy shares the origin repository's git object store
    /// (cheap — e.g. branch or worktree) rather than producing a full separate copy (e.g. fork or clone).
    /// </summary>
    bool SharesObjectStore { get; }

    /// <summary>
    /// Materializes the isolated copy described by <paramref name="request"/> using the supplied engine.
    /// </summary>
    /// <param name="engine">The local-git engine that performs the underlying operations.</param>
    /// <param name="request">The isolation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the created <see cref="IsolatedCopy"/>, or a failure.</returns>
    Task<IGenericResult<IsolatedCopy>> Materialize(IWorktreeEngine engine, IsolationRequest request, CancellationToken cancellationToken = default);
}
