namespace Fdw.Services.ExternalIdentityProviders.Abstractions.Models;

/// <summary>
/// The login-discovery view of an active external identity provider — the minimal, public subset a
/// login page needs to render the provider option and start the browser authorization flow. Returned
/// by the API's <c>GET /auth/external-identity-providers</c> so the UI never opens ConfigurationDb.
/// </summary>
/// <remarks>
/// Deliberately excludes every secret and validation-only field: <c>SecretManagerName</c>,
/// <c>SecretKeyName</c>, <c>Audience</c>, and all row identity/FK columns. These
/// fields (Authority, ClientId, discovery URL) are already public — they appear in the browser's
/// authorization request — so exposing them to a login page discloses nothing new. The client secret is
/// never part of this contract.
/// </remarks>
public sealed class ExternalIdentityProviderSummaryDto
{
    /// <summary>Gets or sets the provider configuration name (the <c>provider</c> value posted back on the external_identity grant).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the provider type discriminator (the ServiceTypeOption name, e.g. <c>Oidc</c>).</summary>
    public string ProviderType { get; set; } = string.Empty;

    /// <summary>Gets or sets the authority/issuer URI the UI uses to build the authorization redirect and discover endpoints.</summary>
    public string? Authority { get; set; }

    /// <summary>Gets or sets the public OAuth2/OIDC client id this relying party is registered as with the external IdP.</summary>
    public string? ClientId { get; set; }

    /// <summary>Gets or sets the explicit OIDC discovery document address, when the provider sets one instead of deriving it from <see cref="Authority"/>.</summary>
    public string? MetadataAddress { get; set; }
}
