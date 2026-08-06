namespace Fdw.Services.SecretManagers.Endpoints;

/// <summary>
/// DTO for a registered secret manager type returned by ListSecretManagerTypesEndpointBase.
/// </summary>
public class SecretManagerTypeDto
{
    /// <summary>Gets or sets the type name key (e.g. "EnvironmentVariable", "MsSql").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a short human-readable description.</summary>
    public string Description { get; set; } = string.Empty;
}
