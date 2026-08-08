using Fdw.Collections;
using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// A session context a connection kind can declare support for: the identity of one way a
/// connection may describe the calling principal to the store it opens.
/// </summary>
/// <remarks>
/// <para>
/// <b>Kind-agnostic members only.</b> What a session context <i>applies</i> is necessarily
/// kind-specific — the reference SQL Server scheme writes <c>SESSION_CONTEXT</c> keys onto an open
/// <c>SqlConnection</c> via <c>MsSqlSessionContextBase.Apply</c> — and hoisting that here would put a
/// <c>Microsoft.Data.SqlClient.SqlConnection</c> into an abstraction shared by every connection
/// kind, including kinds that have no session-context concept at all. The two members below are
/// declared here precisely because neither mentions a kind-specific type: both take only an
/// <see cref="IAuthenticationContext"/> and return a primitive. Nothing kind-specific crosses the
/// boundary, so no consumer learns what kind it is holding by calling them.
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
    /// <summary>
    /// Gets a value indicating whether this session context governs
    /// <paramref name="authenticationContext"/> — that is, whether this is the option the scheme
    /// would apply for that caller.
    /// </summary>
    /// <remarks>
    /// A scheme's options must partition the space of authentication contexts exhaustively and
    /// exclusively, including <see langword="null"/>: exactly one option governs any given input.
    /// That invariant is what lets a caller select with <c>Single</c> rather than an ordered
    /// if-else chain with a "none matched" fallback, and it is why a deny-style option's predicate
    /// should be written as the explicit complement of its siblings rather than left as a catch-all.
    /// </remarks>
    /// <param name="authenticationContext">
    /// The authentication context of the current logical call flow, or <see langword="null"/> when
    /// none has been established.
    /// </param>
    bool Governs(IAuthenticationContext? authenticationContext);

    /// <summary>
    /// Gets the cache partition this session context produces for
    /// <paramref name="authenticationContext"/> — an opaque token identifying the visibility scope
    /// the resulting session will read under.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this belongs on the session context.</b> Result caches sit <i>above</i> the connection
    /// layer, but what a caller may see is decided <i>at</i> it. A cache keyed only on query shape
    /// serves one caller's filtered result to another, so the key must carry whatever distinguishes
    /// visibility. Only the scheme knows what that is, so only the scheme can state it.
    /// </para>
    /// <para>
    /// <b>Opaque to every consumer.</b> The returned string is for equality and concatenation only.
    /// Callers must never parse it, branch on it, or infer a connection kind, principal or tenant
    /// from it — doing so would relearn the scheme the connection layer exists to hide. Two callers
    /// that receive equal tokens may share cached results; two that do not, must not.
    /// </para>
    /// <para>
    /// <b>A kind with no session-context concept returns a constant.</b> Such a connection never
    /// describes the calling principal to its store, so the store cannot vary its answer by caller
    /// and every caller shares one partition. That is a declared property of the scheme, not an
    /// absence to be defaulted.
    /// </para>
    /// </remarks>
    /// <param name="authenticationContext">
    /// The authentication context of the current logical call flow, or <see langword="null"/> when
    /// none has been established. This is the same input <see cref="Governs"/> selected on.
    /// </param>
    string CachePartition(IAuthenticationContext? authenticationContext);
}
