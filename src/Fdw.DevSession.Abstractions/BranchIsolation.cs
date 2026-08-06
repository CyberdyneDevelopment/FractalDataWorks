using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// Isolation via a branch only (no separate working tree), sharing the origin's object store. The
/// lightest strategy — suitable when work happens in-place on the current checkout or the engine
/// swaps refs without a second tree.
/// </summary>
[TypeOption(typeof(IsolationLevels), "Branch")]
[ExcludeFromCodeCoverage]
public sealed class BranchIsolation : IsolationLevelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BranchIsolation"/> class.
    /// </summary>
    public BranchIsolation()
        : base(id: 2, name: "Branch", sharesObjectStore: true)
    {
    }

    /// <inheritdoc />
    public override Task<IGenericResult<IsolatedCopy>> Materialize(IWorktreeEngine engine, IsolationRequest request, CancellationToken cancellationToken = default)
        => engine.CreateBranch(request, cancellationToken);
}
