namespace Fdw.Services.SecretManagers.Endpoints;

/// <summary>
/// Request to get a secret manager by name.
/// </summary>
public sealed class GetSecretManagerRequest
{
    /// <summary>Gets or sets the secret manager name (from route).</summary>
    public string Name { get; set; } = string.Empty;
}
