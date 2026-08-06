using System.Diagnostics.CodeAnalysis;

namespace Fdw.Hosting.Abstractions.Configuration;

/// <summary>
/// Authentication options for database connections.
/// </summary>
// Why: pure DTO, only auto-properties bound from IOptions, no logic.
[ExcludeFromCodeCoverage]
public class AuthenticationOptions
{
    /// <summary>
    /// Gets or sets the authentication type: "SqlAuth", "WindowsAuth", "AzureAD", "ManagedIdentity".
    /// Default is "SqlAuth".
    /// </summary>
    public string Type { get; set; } = "SqlAuth";

    /// <summary>
    /// Gets or sets the username for SQL authentication.
    /// </summary>
    /// <remarks>
    /// Should be set via environment variable: FdwHost__Configuration__Connection__Authentication__Username
    /// </remarks>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the password for SQL authentication.
    /// </summary>
    /// <remarks>
    /// MUST be set via environment variable: FdwHost__Configuration__Connection__Authentication__Password
    /// Never store passwords in appsettings.json files.
    /// </remarks>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the Azure AD tenant ID for Azure AD authentication.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the client ID for Azure AD app authentication.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the client secret for Azure AD app authentication.
    /// </summary>
    /// <remarks>
    /// MUST be set via environment variable if used.
    /// </remarks>
    public string? ClientSecret { get; set; }
}
