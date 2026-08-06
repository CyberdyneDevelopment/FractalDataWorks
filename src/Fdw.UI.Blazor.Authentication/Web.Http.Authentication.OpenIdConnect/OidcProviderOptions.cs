namespace Fdw.Web.Http.Authentication.OpenIdConnect;

using System.Collections.Generic;

/// <summary>
/// Configuration options for an OpenID Connect provider.
/// </summary>
public sealed class OidcProviderOptions
{
    /// <summary>
    /// Gets or sets the OIDC authority URL (issuer).
    /// </summary>
    public string Authority { get; set; } = "";

    /// <summary>
    /// Gets or sets the client ID registered with the provider.
    /// </summary>
    public string ClientId { get; set; } = "";

    /// <summary>
    /// Gets or sets the client secret, if applicable (confidential clients).
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the requested scopes.
    /// </summary>
    public IList<string> Scopes { get; set; } = new List<string> { "openid", "profile", "email" };

    /// <summary>
    /// Gets or sets the callback path for the OIDC redirect.
    /// </summary>
    public string CallbackPath { get; set; } = "/signin-oidc";

    /// <summary>
    /// Gets or sets the signed-out callback path.
    /// </summary>
    public string SignedOutCallbackPath { get; set; } = "/signout-callback-oidc";

    /// <summary>
    /// Gets or sets the response type. Defaults to "code" for authorization code flow.
    /// </summary>
    public string ResponseType { get; set; } = "code";

    /// <summary>
    /// Gets or sets a value indicating whether HTTPS metadata is required.
    /// Defaults to <c>true</c> for production; set to <c>false</c> for local development.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// Gets or sets the password recovery URL template for the provider.
    /// Use <c>{authority}</c> as a placeholder for the authority URL.
    /// </summary>
    public string? PasswordRecoveryUrlTemplate { get; set; }

    /// <summary>
    /// Gets or sets the display name for the provider.
    /// </summary>
    public string DisplayName { get; set; } = "OpenID Connect";
}
