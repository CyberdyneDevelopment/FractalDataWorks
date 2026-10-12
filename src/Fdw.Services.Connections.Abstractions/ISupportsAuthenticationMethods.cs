using System.Collections.Generic;
using Fdw.Services.Connections.Abstractions.Authentication;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Opt-in capability interface for connection types that sign in with one of several authentication methods.
/// </summary>
/// <remarks>
/// A connection type implements this to list the methods a connection of its kind may name, each with the
/// credentials it asks for and whether it acts as the host, so a create form can ask for them before the connection
/// exists (FDW-1090). Consistent with <see cref="ISupportsProtocols"/>; a type that does not implement it offers no
/// authentication methods.
/// </remarks>
public interface ISupportsAuthenticationMethods
{
    /// <summary>Gets the authentication methods a connection of this kind may name.</summary>
    IReadOnlyList<ConnectionAuthenticationMethod> AuthenticationMethods { get; }
}
