namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Holds the name of the platform's own configuration connection, the one the platform's configuration rows are read from
/// and written to. Every domain and every endpoint that needs it reads it here; none carries its own copy.
/// </summary>
/// <remarks>
/// There is no default. The host declares the name as <c>ConnectionRoles.Platform</c> in its <c>configurationSchema.json</c>;
/// the configuration gateway domain's registration reads that declaration, checks it names a connection the same file
/// declares, and refuses to start the host when it is missing. Reading <see cref="Name"/> before that has happened is a
/// defect in the caller and throws.
/// </remarks>
public static class PlatformConnection
{
    private static readonly DeclaredConnectionName _declared = new("Platform");

    /// <summary>Gets the name of the platform's own configuration connection, as the host declared it.</summary>
    public static string Name => _declared.Name;

    /// <summary>Gets a value indicating whether the host has declared the platform connection.</summary>
    public static bool IsDeclared => _declared.IsDeclared;

    /// <summary>Records the host's declaration. Called by the configuration gateway domain's registration only.</summary>
    /// <param name="name">The declared connection name.</param>
    /// <returns><see langword="false"/> when a different name was already recorded.</returns>
    public static bool Declare(string name) => _declared.Declare(name);
}
