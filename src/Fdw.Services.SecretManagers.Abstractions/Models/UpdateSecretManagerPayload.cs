namespace Fdw.Services.SecretManagers.Clients.Models;

/// <summary>
/// Request payload for updating an existing secret manager configuration.
/// </summary>
public sealed class UpdateSecretManagerPayload
{
    /// <summary>Gets or sets the new description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the new environment.</summary>
    public string? Environment { get; set; }
}
