using Fdw.Collections;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Option base for <see cref="NoSessionContextTypes"/> — the collection a connection kind declares
/// when it has no session-context concept at all.
/// </summary>
/// <remarks>
/// This base declares no behavioral member because there is no behavior to declare: a kind whose
/// session contexts come from this collection never describes a calling principal to the store it
/// opens. It exists so the "no session context" position is a real, named member of a real
/// collection rather than an empty list, which would be indistinguishable from a kind that simply
/// forgot to declare its contexts.
/// </remarks>
public abstract class NoSessionContextBase : TypeOptionBase<int, ISessionContext>, ISessionContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoSessionContextBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this session context.</param>
    /// <param name="name">The name of this session context.</param>
    protected NoSessionContextBase(int id, string name) : base(id, name)
    {
    }
}
