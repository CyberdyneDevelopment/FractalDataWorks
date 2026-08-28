namespace Fdw.Services.Multitenancy.Clients.Models;

/// <summary>
/// Response for tenant switch operation.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class SwitchTenantResponse
{
    /// <summary>
    /// Gets or sets whether the switch was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the new access token with updated tenant claim.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the new refresh token.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Gets or sets the token expiration in seconds.
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Gets or sets the result message.
    /// </summary>
    public string? Message { get; set; }
}
