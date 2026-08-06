namespace Fdw.Services.SecretManagers.Clients.Models;

/// <summary>
/// Summary payload for a secret manager configuration.
/// </summary>
public sealed class SecretManagerSummaryPayload
{
    /// <summary>Gets or sets the configuration name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the secret manager type.</summary>
    public string? SecretManagerType { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }
}
