namespace Fdw.Services.SecretManagers.Clients.Models;

/// <summary>
/// Request payload for creating a new secret manager configuration.
/// </summary>
public sealed class CreateSecretManagerPayload
{
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the secret manager type (e.g., "EnvironmentVariable", "MsSql", "AzureKeyVault").</summary>
    public string SecretManagerType { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the environment.</summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Gets or sets the typed-body configuration for the chosen <see cref="SecretManagerType"/>.
    /// The server validator requires this field to be non-null. Pass at minimum an empty object
    /// so the server can deserialize and persist the typed-body child row with default values.
    /// </summary>
    public object? Configuration { get; set; }
}
