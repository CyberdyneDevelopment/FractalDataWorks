namespace Fdw.Services.SecretManagers.Endpoints;

/// <summary>
/// Request to update an existing secret manager configuration.
/// </summary>
public sealed class UpdateSecretManagerRequest
{
    /// <summary>Gets or sets the secret manager name (from route).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the new description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the new environment.</summary>
    public string? Environment { get; set; }
}
