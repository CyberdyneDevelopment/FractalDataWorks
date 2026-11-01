namespace Fdw.Services.Users;

/// <summary>
/// Configuration options for the Users service domain, bound from the "Users" configuration section.
/// </summary>
public sealed class UsersServiceOptions
{
    /// <summary>
    /// Gets or sets the name of the credential service that stores user credentials.
    /// Required — a missing or blank value is a configuration error surfaced as a Critical
    /// MessageLogging failure at the service layer.
    /// </summary>
    public string? CredentialServiceName { get; set; }
}
