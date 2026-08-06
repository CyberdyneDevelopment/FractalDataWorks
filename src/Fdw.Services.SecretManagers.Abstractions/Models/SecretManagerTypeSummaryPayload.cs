namespace Fdw.Services.SecretManagers.Clients.Models;

/// <summary>
/// Summary payload for a registered secret manager type.
/// </summary>
public sealed class SecretManagerTypeSummaryPayload
{
    /// <summary>Gets or sets the type name key (e.g. "EnvironmentVariable", "MsSql").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a short human-readable description.</summary>
    public string Description { get; set; } = string.Empty;
}
