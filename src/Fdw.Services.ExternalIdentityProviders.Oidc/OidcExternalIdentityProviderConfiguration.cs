using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Abstractions.Models;

namespace Fdw.Services.ExternalIdentityProviders.Oidc;

/// <summary>
/// Typed-body configuration for the Oidc external-identity-provider TypeOption.
/// Standalone POCO — does NOT inherit from <see cref="ExternalIdentityProviderConfiguration"/>.
/// Persisted to <c>auth.OidcExternalIdentityProvider</c> as a child of
/// <c>auth.ExternalIdentityProvider</c> via <see cref="ExternalIdentityProviderId"/>.
///
/// Base fields (Name, SecretManagerName, SecretKeyName, etc.) remain on the parent header row
/// (<see cref="ExternalIdentityProviderConfiguration"/>). The header provider
/// (<see cref="ExternalIdentityProviderConfigurationProvider"/>) loads the header, then dispatches to
/// the Oidc typed provider to load this row by <see cref="ExternalIdentityProviderId"/>.
/// <c>ComposeTypedBody</c> sets <c>header.Configuration = typedBody</c> and returns the header, so
/// callers read typed fields via <c>PopulateSummary</c> or by casting to this type.
///
/// This POCO and its ConfigurationCommand are the CONTRACT and stay in FDW. The aggregation that
/// consumes them — the provider service, its factory, the <c>[ServiceTypeOption]</c> and the typed
/// configuration provider — lives in
/// <c>reference-servicetypes/ReferenceExternalIdentityProviders.Oidc</c> under FDW-622's boundary
/// rule, which is why those types are deliberately not <c>cref</c>'d here.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "ExternalIdentityProvider", ServiceType = "Oidc")]
public sealed partial class OidcExternalIdentityProviderConfiguration : IExternalIdentityProviderConfiguration
{
    // ========================================
    // IGenericConfiguration (typed-body identity)
    // ========================================

    /// <summary>
    /// Gets or sets the unique identifier for this typed-body row
    /// (<c>auth.OidcExternalIdentityProvider.Id</c>).
    /// </summary>
    // Why: No Guid.NewGuid() default — the provider mints this before INSERT.
    public Guid Id { get; set; }

    // Why: IGenericConfiguration members below satisfy the interface contract via EXPLICIT
    // interface implementation so [GenerateMapper] does NOT map them — they are not columns on
    // auth.OidcExternalIdentityProvider. The canonical Name/SectionName/ServiceType/
    // ServiceOptionType live on the parent ExternalIdentityProviderConfiguration row; the typed body
    // is identified solely by ExternalIdentityProviderId. Mirrors OpenIddictTokenManagerConfiguration.
    string IGenericConfiguration.Name
    {
        get => string.Empty;
        set { /* typed body has no independent name — identified by ExternalIdentityProviderId */ }
    }

    string IGenericConfiguration.SectionName => "ExternalIdentityProviders";
    string IGenericConfiguration.ServiceType => "ExternalIdentityProvider";
    string? IGenericConfiguration.ServiceOptionType => "Oidc";

    // ========================================
    // FK to header
    // ========================================

    /// <summary>
    /// Gets or sets the durable logical FK to <c>auth.ExternalIdentityProvider.Id</c> (the parent header).
    /// </summary>
    public Guid ExternalIdentityProviderId { get; set; }

    // ========================================
    // Oidc-specific fields
    // ========================================

    /// <summary>
    /// Gets or sets the authority (issuer) URI used to validate the JWT <c>iss</c> claim and to
    /// build the default discovery document address when <see cref="MetadataAddress"/> is not set.
    /// Example: <c>https://login.microsoftonline.com/{tenant}/v2.0</c>.
    /// </summary>
    // Why: no fallback default — a missing Authority is a structured, logged validation failure
    // (see OidcExternalIdentityProvider.ValidateExternalToken), never a silently assumed value.
    public string? Authority { get; set; }

    /// <summary>
    /// Gets or sets the OAuth2/OIDC client id this relying party is registered as with the external
    /// IdP. Used to validate the JWT <c>aud</c> claim when <see cref="Audience"/> is not separately set.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the expected audience for token validation. When null, <see cref="ClientId"/> is
    /// used as the audience (the common case for a single-audience OIDC client).
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    /// Gets or sets an explicit OIDC discovery document address. When empty, the well-known discovery
    /// path is derived from <see cref="Authority"/> (<c>{Authority}/.well-known/openid-configuration</c>).
    /// </summary>
    public string? MetadataAddress { get; set; }

    /// <inheritdoc />
    // Why: the Oidc option projects its OWN public fields for login-time discovery, so
    // GetExternalIdentityProvidersEndpointBase never down-casts to this concrete type and
    // Fdw.Web.Api carries no reference to this package (FDW-624). Secrets are never surfaced —
    // the client secret is resolved at runtime via SecretManagerName/SecretKeyName and is not a
    // property on this row.
    public void PopulateSummary(ExternalIdentityProviderSummaryDto summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        summary.Authority = Authority;
        summary.ClientId = ClientId;
        summary.MetadataAddress = MetadataAddress;
    }
}
