using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// Isolation via a git worktree: a separate checked-out working tree on a new branch, sharing the
/// origin's object store. The cheap default — no full copy — and the natural fit for parallel
/// strands within one session.
/// </summary>
[TypeOption(typeof(IsolationLevels), "Worktree")]
[ExcludeFromCodeCoverage]
public sealed class WorktreeIsolation : IsolationLevelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorktreeIsolation"/> class.
    /// </summary>
    public WorktreeIsolation()
        : base(id: 1, name: "Worktree", sharesObjectStore: true)
    {
    }

    /// <inheritdoc />
    public override Task<IGenericResult<IsolatedCopy>> Materialize(IWorktreeEngine engine, IsolationRequest request, CancellationToken cancellationToken = default)
        => engine.CreateWorktree(request, cancellationToken);
}
