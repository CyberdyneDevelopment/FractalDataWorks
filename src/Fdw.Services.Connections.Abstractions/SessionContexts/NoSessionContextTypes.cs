using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// The session contexts of a connection kind that has no session-context concept — the starting
/// position every connection type holds until it declares otherwise.
/// </summary>
/// <remarks>
/// <para>
/// This is the base default of <c>IConnectionType.SessionContextTypes</c>. It is a real collection
/// with a real member (<see cref="NoneSessionContext"/>), not a placeholder: "this kind does not
/// carry a session context" is a position a connection type states, not an absence a reader has to
/// infer. There is deliberately no companion <c>bool</c> predicate — support is read off the
/// collection's members and never off a second signal that could disagree with them.
/// </para>
/// <para>
/// A new connection implementation therefore declares what it actually supports and never silently
/// acquires another scheme's semantics. That matters specifically because the reference SQL Server
/// scheme's system elevation <i>is</i> the absence of a key: if an undeclared kind fell through to
/// "set nothing", it would be indistinguishable on the wire from full system elevation.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(NoSessionContextBase), typeof(ISessionContext), typeof(NoSessionContextTypes))]
public abstract partial class NoSessionContextTypes : TypeCollectionBase<NoSessionContextBase, ISessionContext>
{
}
