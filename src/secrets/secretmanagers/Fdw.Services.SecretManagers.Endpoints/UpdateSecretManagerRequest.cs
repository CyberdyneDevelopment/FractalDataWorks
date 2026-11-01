using System.Text.Json;

namespace Fdw.Services.SecretManagers.Endpoints;

/// <summary>
/// Request to update an existing secret manager configuration.
/// </summary>
public sealed class UpdateSecretManagerRequest
{
    /// <summary>Gets or sets the secret manager name (from route).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the settings to change, as a JSON object of the members of the secret manager's own type. Each sent member is laid over the stored
    /// settings and the rest keep their values. A member the type does not take is refused, naming it and listing those it takes.
    /// </summary>
    public JsonElement? Configuration { get; set; }
}
