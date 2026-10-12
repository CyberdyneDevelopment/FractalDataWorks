using System;
using System.Collections.Generic;

namespace Fdw.Services.Connections.Abstractions.Authentication;

/// <summary>
/// One authentication method a connection kind offers, described for someone about to create a connection with it:
/// its name, whether it acts as the host, and the credentials it asks for.
/// </summary>
/// <remarks>
/// Read from the method's own declarations (<see cref="ConnectionAuthenticationTypeBase{TSelf}.Credentials"/> and
/// <see cref="ConnectionAuthenticationTypeBase{TSelf}.UsesHostIdentity"/>), so a create form can ask for every
/// credential before the connection exists (FDW-1090).
/// </remarks>
public sealed class ConnectionAuthenticationMethod
{
    /// <summary>Initializes a new instance of the <see cref="ConnectionAuthenticationMethod"/> class.</summary>
    /// <param name="name">The method's name, as a connection names it.</param>
    /// <param name="displayName">The method's display name.</param>
    /// <param name="usesHostIdentity">Whether the method, with none of its own options set, acts as the host.</param>
    /// <param name="credentials">The credentials the method asks for.</param>
    public ConnectionAuthenticationMethod(
        string name, string displayName, bool usesHostIdentity, IReadOnlyList<ConnectionCredentialField> credentials)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        UsesHostIdentity = usesHostIdentity;
        Credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
    }

    /// <summary>Gets the method's name, as a connection names it.</summary>
    public string Name { get; }

    /// <summary>Gets the method's display name.</summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets a value indicating whether the method, with none of its own options set, acts as the host -- which makes
    /// creating a connection with it a platform administrator's decision (FDW-1068). A method whose answer depends on
    /// its options (MsSql EntraId in ServicePrincipal mode signs in as itself) states the answer for no options.
    /// </summary>
    public bool UsesHostIdentity { get; }

    /// <summary>Gets the credentials the method asks for; empty for a method that carries none.</summary>
    public IReadOnlyList<ConnectionCredentialField> Credentials { get; }

    /// <summary>Describes an authentication method from its own declarations.</summary>
    /// <typeparam name="TSelf">The method's own type.</typeparam>
    /// <param name="method">The authentication method.</param>
    /// <returns>The description.</returns>
    public static ConnectionAuthenticationMethod Of<TSelf>(ConnectionAuthenticationTypeBase<TSelf> method)
        where TSelf : Fdw.Collections.ITypeOption<int, TSelf>
    {
        if (method is null) throw new ArgumentNullException(nameof(method));
        return new ConnectionAuthenticationMethod(
            method.Name,
            method.DisplayName,
            method.UsesHostIdentity(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)),
            method.Credentials);
    }
}
