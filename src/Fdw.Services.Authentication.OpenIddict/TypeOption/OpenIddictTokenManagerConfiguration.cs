using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.TokenManagers.Abstractions;

namespace Fdw.Services.Authentication.OpenIddict;

/// <summary>
/// Typed-body configuration for the OpenIddict token-manager TypeOption.
/// Standalone POCO — does NOT inherit from <see cref="Fdw.Services.TokenManagers.TokenManagerConfiguration"/>.
/// Persisted to <c>auth.OpenIddictTokenManager</c> as a child of
/// <c>auth.TokenManager</c> via <see cref="TokenManagerId"/>.
///
/// Base fields (Name, SecretManagerName, SecretKeyName, etc.) remain on the parent header
/// row (<see cref="Fdw.Services.TokenManagers.TokenManagerConfiguration"/>). The header provider
/// (<see cref="Fdw.Services.TokenManagers.TokenManagerConfigurationProvider"/>) loads the header, then
/// dispatches to the typed provider (<c>OpenIddictTokenManagerConfigurationProvider</c> (ReferenceAuthentication.OpenIddict))
/// to load this row by <see cref="TokenManagerId"/>. <c>PopulateTypedBody</c> sets
/// <c>header.Configuration = typedBody</c> and returns the header, so callers read typed fields
/// via <c>(header.Configuration as OpenIddictTokenManagerConfiguration)</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "TokenManager", ServiceType = "OpenIddict")]
public sealed partial class OpenIddictTokenManagerConfiguration : ITokenManagerConfiguration
{
    // ========================================
    // IGenericConfiguration (typed-body identity)
    // ========================================

    /// <summary>
    /// Gets or sets the unique identifier for this typed-body row
    /// (<c>auth.OpenIddictTokenManager.Id</c>).
    /// </summary>
    // Why: No Guid.NewGuid() default — the provider mints this before INSERT.
    public Guid Id { get; set; }

    // Why: IGenericConfiguration members below satisfy the interface contract via EXPLICIT
    // interface implementation so [GenerateMapper] does NOT map them — they are not columns on
    // auth.OpenIddictTokenManager. The canonical Name/SectionName/ServiceType/
    // ServiceOptionType live on the parent TokenManagerConfiguration row; the typed body
    // is identified solely by TokenManagerId. Mirrors MsSqlConnectionConfiguration.
    string IGenericConfiguration.Name
    {
        get => string.Empty;
        set { /* typed body has no independent name — identified by TokenManagerId */ }
    }

    string IGenericConfiguration.SectionName => "TokenManagers";
    string IGenericConfiguration.ServiceType => "TokenManager";
    string? IGenericConfiguration.ServiceOptionType => "OpenIddict";

    // ========================================
    // FK to header
    // ========================================

    /// <summary>
    /// Gets or sets the durable logical FK to <c>auth.TokenManager.Id</c> (the parent header).
    /// </summary>
    public Guid TokenManagerId { get; set; }

    // ========================================
    // OpenIddict-specific fields
    // ========================================

    /// <summary>
    /// Gets or sets the authority (issuer) URI used in the JWT <c>iss</c> claim
    /// and in the OpenID Configuration discovery document.
    /// Example: <c>https://api.example.com</c>.
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token endpoint URI (absolute or relative) used when resolving
    /// outbound credential requests. Relative paths are resolved against <see cref="Authority"/>.
    /// When empty, <c>/connect/token</c> is used.
    /// </summary>
    public string TokenEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the access token lifetime (ISO 8601 duration).
    /// When empty, OpenIddict's built-in server default applies.
    /// Applied via <c>PostConfigure&lt;OpenIddictServerOptions&gt;</c>.
    /// </summary>
    public string AccessTokenLifetime { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the refresh token lifetime (ISO 8601 duration).
    /// When empty, OpenIddict's built-in server default applies.
    /// Applied via <c>PostConfigure&lt;OpenIddictServerOptions&gt;</c>.
    /// </summary>
    public string RefreshTokenLifetime { get; set; } = string.Empty;
}
