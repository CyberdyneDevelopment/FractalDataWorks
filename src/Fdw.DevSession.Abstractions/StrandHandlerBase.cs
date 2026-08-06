using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// CRTP base class for <see cref="IStrandHandler"/> options. Each concrete handler supplies its id and
/// name and its own <see cref="CanHandle"/> and <see cref="Handle"/> behavior.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class StrandHandlerBase : TypeOptionBase<int, StrandHandlerBase>, IStrandHandler
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StrandHandlerBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The handler name.</param>
    protected StrandHandlerBase(int id, string name)
        : base(id, name)
    {
    }

    /// <inheritdoc />
    public abstract bool CanHandle(StrandInfo strand);

    /// <inheritdoc />
    public abstract Task<IGenericResult> Handle(IDevSession session, StrandInfo strand, CancellationToken cancellationToken = default);
}
