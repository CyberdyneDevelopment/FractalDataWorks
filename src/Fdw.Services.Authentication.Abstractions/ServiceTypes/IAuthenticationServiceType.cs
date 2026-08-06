using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// Interface for authentication service type definitions.
/// Provides authentication-specific metadata on top of the base service type.
/// </summary>
public interface IAuthenticationServiceType : ITypeOption<int, IAuthenticationServiceType>, IServiceType
{
    /// <summary>
    /// Gets the authentication protocols supported by this provider.
    /// </summary>
    /// <value>An array of authentication protocol identifiers this provider can handle.</value>
    /// <remarks>
    /// Examples: ["OAuth2", "OpenIDConnect", "SAML2"] for Azure Entra,
    /// ["OAuth2", "OpenIDConnect"] for generic OIDC,
    /// ["SAML2", "WS-Federation"] for ADFS.
    /// </remarks>
    string[] SupportedProtocols { get; }

    /// <summary>
    /// Gets the provider name for this authentication type.
    /// </summary>
    /// <value>The technical name of the underlying provider or framework.</value>
    /// <remarks>
    /// Examples: "Microsoft.Identity.Client", "IdentityServer", "Auth0".
    /// Used for diagnostics, logging, and provider-specific behavior.
    /// </remarks>
    string ProviderName { get; }

    /// <summary>
    /// Gets the authentication flows supported by this provider.
    /// </summary>
    /// <value>A read-only list of supported authentication flows.</value>
    /// <remarks>
    /// Common flows include:
    /// - "AuthorizationCode": Authorization code flow with PKCE
    /// - "ClientCredentials": Client credentials flow for service-to-service
    /// - "DeviceCode": Device code flow for devices without browsers
    /// - "Interactive": Interactive authentication with user involvement
    /// - "Silent": Silent token acquisition using cached tokens
    /// - "OnBehalfOf": On-behalf-of flow for middle-tier services
    /// </remarks>
    IReadOnlyList<string> SupportedFlows { get; }

    /// <summary>
    /// Gets the token types supported by this provider.
    /// </summary>
    /// <value>A read-only list of supported token types.</value>
    /// <remarks>
    /// Common token types include:
    /// - "AccessToken": OAuth2 access tokens
    /// - "IdToken": OpenID Connect ID tokens
    /// - "RefreshToken": OAuth2 refresh tokens
    /// - "SAMLAssertion": SAML assertion tokens
    /// </remarks>
    IReadOnlyList<string> SupportedTokenTypes { get; }

    /// <summary>
    /// Gets the priority of this authentication provider.
    /// </summary>
    /// <value>An integer representing selection priority (higher values = higher priority).</value>
    /// <remarks>
    /// When multiple authentication providers support the same protocol,
    /// the authentication service selects the one with the highest priority.
    /// Typical values: 0-100 (100 being highest priority).
    /// </remarks>
    int Priority { get; }

    /// <summary>
    /// Gets a value indicating whether this provider supports multi-tenant scenarios.
    /// </summary>
    bool SupportsMultiTenant { get; }

    /// <summary>
    /// Gets a value indicating whether this provider supports token caching.
    /// </summary>
    bool SupportsTokenCaching { get; }
}