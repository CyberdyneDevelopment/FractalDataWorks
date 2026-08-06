namespace Fdw.Services.SecretManagers.Clients.Models;

/// <summary>
/// Request DTO for storing a secret in a named secret manager.
/// </summary>
public sealed class SetSecretRequest
{
    /// <summary>Gets or sets the key name to store the secret under.</summary>
    public string KeyName { get; set; } = string.Empty;

    /// <summary>Gets or sets the secret value to store.</summary>
    public string Value { get; set; } = string.Empty;
}
