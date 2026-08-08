using Fdw.Collections;
using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Option base for <see cref="NoSessionContextTypes"/> — the collection a connection kind declares
/// when it has no session-context concept at all.
/// </summary>
/// <remarks>
/// This base declares no <i>applying</i> behavior because there is none to declare: a kind whose
/// session contexts come from this collection never describes a calling principal to the store it
/// opens. It exists so the "no session context" position is a real, named member of a real
/// collection rather than an empty list, which would be indistinguishable from a kind that simply
/// forgot to declare its contexts.
/// </remarks>
public abstract class NoSessionContextBase : TypeOptionBase<int, ISessionContext>, ISessionContext
{
    /// <summary>
    /// The partition every caller through a kind with no session-context concept shares.
    /// </summary>
    /// <remarks>
    /// A literal rather than an empty string so it is visible and greppable in a dumped cache key —
    /// an empty segment reads as a bug, a named one reads as the declaration it is.
    /// </remarks>
    private const string NoSessionContextPartition = "nosc";

    /// <summary>
    /// Initializes a new instance of the <see cref="NoSessionContextBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this session context.</param>
    /// <param name="name">The name of this session context.</param>
    protected NoSessionContextBase(int id, string name) : base(id, name)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// Non-virtual and unconditionally <see langword="true"/>: this collection has exactly one
    /// member, so it governs every authentication context by definition. That satisfies the
    /// exhaustive-and-exclusive partition the interface requires — trivially, with a partition of
    /// one.
    /// </remarks>
    public bool Governs(IAuthenticationContext? authenticationContext) => true;

    /// <inheritdoc />
    /// <remarks>
    /// Non-virtual and constant. The kind never tells its store who is calling, so the store cannot
    /// vary its answer by caller and every caller may share one cached result. Returning the same
    /// token for every input is the whole statement — it is not a fallback for an input this option
    /// failed to interpret.
    /// </remarks>
    public string CachePartition(IAuthenticationContext? authenticationContext)
        => NoSessionContextPartition;
}
