namespace Fdw.Services.SecretManagers.Clients.Models;

/// <summary>
/// Response DTO after storing a secret in a secret manager.
/// </summary>
public sealed class SetSecretResponse
{
    /// <summary>Gets or sets the key name the secret was stored under.</summary>
    public string KeyName { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the secret manager used.</summary>
    public string ManagerName { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the secret was stored successfully.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets a message describing the result.</summary>
    public string Message { get; set; } = string.Empty;
}
