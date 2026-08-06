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
    // Why: No default value — a missing service name must fail loud, not silently route to the
    // wrong service or skip credential storage entirely. Matches the no-fallback rule. This is a
    // POINTER (the connections→secret-managers pattern): Users injects ICredentialServiceProvider
    // and resolves the configured credential service by this name.
    public string? CredentialServiceName { get; set; }
}
