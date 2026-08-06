using Fdw.Collections;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// A session context a connection kind can declare support for: the identity of one way a
/// connection may describe the calling principal to the store it opens.
/// </summary>
/// <remarks>
/// <para>
/// <b>Identity only, deliberately.</b> This interface carries no behavioral member. What a session
/// context actually <i>does</i> is necessarily kind-specific — the reference SQL Server scheme
/// writes <c>SESSION_CONTEXT</c> keys onto an open <c>SqlConnection</c> via
/// <c>MsSqlSessionContextBase.Apply</c> — and hoisting that here would put a
/// <c>Microsoft.Data.SqlClient.SqlConnection</c> into an abstraction shared by every connection
/// kind, including kinds that have no session-context concept at all.
/// </para>
/// <para>
/// <b>Why it derives from <see cref="ITypeOption{TKey,TValue}"/> rather than being a bare marker.</b>
/// <see cref="ITypeOption.Name"/> is the durable identity of the option — the value a future
/// per-connection discriminator column joins on. A bare marker also breaks the collection
/// generator: with no <c>Name</c>/<c>Id</c> reachable on the exposed type it emits always-<c>NotFound</c>
/// <c>ByName</c>/<c>ById</c> stubs, and with one or more registered options it does not compile at
/// all (the generator emits <c>_all!.First(x =&gt; x.Name == …)</c> and
/// <c>ToFrozenDictionary(x =&gt; x.Name)</c> over the exposed type).
/// </para>
/// <para>
/// <b>The collection, not the option, is the unit of replacement.</b> A session-context scheme is a
/// contract with a specific row-level-security design as deployed. A consumer running a different
/// scheme points its connection type's <c>SessionContextTypes</c> at its own collection of
/// <see cref="ISessionContext"/> options rather than adding a member to someone else's. Connection
/// <i>kind</i> and session-context <i>scheme</i> are separate axes: two consumers can both talk to
/// SQL Server and disagree completely about what a session context carries.
/// </para>
/// </remarks>
public interface ISessionContext : ITypeOption<int, ISessionContext>
{
}
