namespace Fdw.Services.SecretManagers.Endpoints;

/// <summary>
/// Response DTO for a secret manager summary.
/// </summary>
public sealed class SecretManagerSummaryResponse
{
    /// <summary>Gets or sets the configuration name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the secret manager type.</summary>
    public string? SecretManagerType { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }
}
