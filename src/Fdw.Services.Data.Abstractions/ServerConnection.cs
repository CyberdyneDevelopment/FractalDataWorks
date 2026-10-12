namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Holds the name of the host's server-tier configuration connection, the store for boot-time and near-static values
/// (hosts, logging, telemetry, trusted issuers). Every domain that reads the server tier reads the name here; none
/// carries its own copy.
/// </summary>
/// <remarks>
/// There is no default. The host declares the name as <c>ConnectionRoles.Server</c> in its <c>configurationSchema.json</c>
/// and start-up refuses a host that does not. Reading <see cref="Name"/> before the declaration is read is a defect in the
/// caller and throws.
/// </remarks>
public static class ServerConnection
{
    private static readonly DeclaredConnectionName _declared = new("Server");

    /// <summary>Gets the name of the server-tier configuration connection, as the host declared it.</summary>
    public static string Name => _declared.Name;

    /// <summary>Gets a value indicating whether the host has declared the server connection.</summary>
    public static bool IsDeclared => _declared.IsDeclared;

    /// <summary>Records the host's declaration. Called by the configuration gateway domain's registration only.</summary>
    /// <param name="name">The declared connection name.</param>
    /// <returns><see langword="false"/> when a different name was already recorded.</returns>
    public static bool Declare(string name) => _declared.Declare(name);
}
