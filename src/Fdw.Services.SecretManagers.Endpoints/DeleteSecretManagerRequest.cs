namespace Fdw.Services.SecretManagers.Endpoints;

/// <summary>
/// Request to delete a secret manager by name.
/// </summary>
public sealed class DeleteSecretManagerRequest
{
    /// <summary>Gets or sets the secret manager name (from route).</summary>
    public string Name { get; set; } = string.Empty;
}
