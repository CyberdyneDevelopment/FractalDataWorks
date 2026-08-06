using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// CRTP base class for <see cref="IIsolationLevel"/> strategies. Each concrete strategy supplies its
/// id, name, object-store-sharing characteristic, and its own <see cref="Materialize"/> behavior.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class IsolationLevelBase : TypeOptionBase<int, IsolationLevelBase>, IIsolationLevel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IsolationLevelBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The strategy name.</param>
    /// <param name="sharesObjectStore">Whether the strategy shares the origin git object store.</param>
    protected IsolationLevelBase(int id, string name, bool sharesObjectStore)
        : base(id, name)
    {
        SharesObjectStore = sharesObjectStore;
    }

    /// <inheritdoc />
    public bool SharesObjectStore { get; }

    /// <inheritdoc />
    public abstract Task<IGenericResult<IsolatedCopy>> Materialize(IWorktreeEngine engine, IsolationRequest request, CancellationToken cancellationToken = default);
}
