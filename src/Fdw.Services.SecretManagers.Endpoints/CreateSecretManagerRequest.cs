using System.Text.Json;

namespace Fdw.Services.SecretManagers.Endpoints;

/// <summary>
/// Request to create a new secret manager configuration.
/// </summary>
public sealed class CreateSecretManagerRequest
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
    /// Typed-body settings for the chosen <see cref="SecretManagerType"/>. Required —
    /// the endpoint deserializes this JSON object into the matching typed-body
    /// configuration (e.g. EnvironmentVariableConfiguration) and persists it alongside
    /// the header row.
    /// </summary>
    public JsonElement? Configuration { get; set; }
}
