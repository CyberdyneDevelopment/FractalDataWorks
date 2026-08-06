namespace Fdw.Web.Http.Authentication.OpenIdConnect;

using System;
using System.Collections.Generic;

/// <summary>
/// Default configuration values for Authentik as an OpenID Connect provider.
/// </summary>
public static class AuthentikDefaults
{
    /// <summary>
    /// The default authentication scheme name for Authentik.
    /// </summary>
    public const string AuthenticationScheme = "Authentik";

    /// <summary>
    /// The display name shown in UI for the Authentik provider.
    /// </summary>
    public const string DisplayName = "Authentik";

    /// <summary>
    /// The default callback path for Authentik OIDC redirects.
    /// </summary>
    public const string CallbackPath = "/signin-authentik";

    /// <summary>
    /// The default signed-out callback path for Authentik.
    /// </summary>
    public const string SignedOutCallbackPath = "/signout-callback-authentik";

    /// <summary>
    /// The default scopes requested from Authentik.
    /// </summary>
    public static IReadOnlyList<string> DefaultScopes { get; } = new[] { "openid", "profile", "email" };

    /// <summary>
    /// Builds the OpenID Connect discovery endpoint URL for an Authentik instance.
    /// </summary>
    /// <param name="authority">The Authentik authority URL (e.g., https://auth.example.com).</param>
    /// <param name="applicationSlug">The Authentik application slug.</param>
    /// <returns>The well-known OpenID configuration URL.</returns>
    public static string GetDiscoveryEndpoint(string authority, string applicationSlug)
    {
        ArgumentNullException.ThrowIfNull(authority);
        ArgumentNullException.ThrowIfNull(applicationSlug);

        var baseUrl = authority.TrimEnd('/');
        return $"{baseUrl}/application/o/{applicationSlug}/.well-known/openid-configuration";
    }

    /// <summary>
    /// Builds the password recovery URL for an Authentik instance.
    /// </summary>
    /// <param name="authority">The Authentik authority URL.</param>
    /// <returns>The password recovery URL.</returns>
    public static string GetPasswordRecoveryUrl(string authority)
    {
        ArgumentNullException.ThrowIfNull(authority);

        var baseUrl = authority.TrimEnd('/');
        return $"{baseUrl}/if/flow/recovery/";
    }

    /// <summary>
    /// Creates an <see cref="OidcProviderOptions"/> pre-configured for Authentik.
    /// </summary>
    /// <param name="authority">The Authentik authority URL.</param>
    /// <param name="clientId">The client ID registered in Authentik.</param>
    /// <param name="clientSecret">The client secret, if applicable.</param>
    /// <returns>A configured <see cref="OidcProviderOptions"/> instance.</returns>
    public static OidcProviderOptions CreateOptions(string authority, string clientId, string? clientSecret = null)
    {
        ArgumentNullException.ThrowIfNull(authority);
        ArgumentNullException.ThrowIfNull(clientId);

        return new OidcProviderOptions
        {
            Authority = authority,
            ClientId = clientId,
            ClientSecret = clientSecret,
            CallbackPath = CallbackPath,
            SignedOutCallbackPath = SignedOutCallbackPath,
            Scopes = new List<string>(DefaultScopes),
            DisplayName = DisplayName,
            PasswordRecoveryUrlTemplate = GetPasswordRecoveryUrl(authority),
        };
    }
}
