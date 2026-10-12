namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Holds the name of the host's authentication connection, the store that holds sessions, flow state and token
/// revocation. Every reader takes the name from here; none carries its own copy.
/// </summary>
/// <remarks>
/// There is no default. A host that authenticates declares the name as <c>ConnectionRoles.Auth</c> in its
/// <c>configurationSchema.json</c>; a host that declares none has no authentication connection, and reading
/// <see cref="Name"/> there is a defect in the caller and throws.
/// </remarks>
public static class AuthConnection
{
    private static readonly DeclaredConnectionName _declared = new("Auth");

    /// <summary>Gets the name of the authentication connection, as the host declared it.</summary>
    public static string Name => _declared.Name;

    /// <summary>Gets a value indicating whether the host has declared the authentication connection.</summary>
    public static bool IsDeclared => _declared.IsDeclared;

    /// <summary>Records the host's declaration. Called by the configuration gateway domain's registration only.</summary>
    /// <param name="name">The declared connection name.</param>
    /// <returns><see langword="false"/> when a different name was already recorded.</returns>
    public static bool Declare(string name) => _declared.Declare(name);
}
