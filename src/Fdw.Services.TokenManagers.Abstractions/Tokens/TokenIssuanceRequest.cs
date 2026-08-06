using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Fdw.Services.TokenManagers.Abstractions.Tokens;

/// <summary>
/// Carries the grant information for a token issuance request.
/// FDW always mints its OWN role/permission token regardless of input path.
/// Two distinct paths are modelled by <see cref="GrantType"/>:
///
/// <list type="number">
///   <item>
///     <term>Credential grant</term>
///     <description>
///       The caller presents a first-party credential (username/password, agent key, …).
///       Populate <see cref="Subject"/> + <see cref="Credential"/>; leave
///       <see cref="ExternalPrincipal"/> null.
///       Common <see cref="GrantType"/> strings: <c>"password"</c>, <c>"agent_key"</c>,
///       <c>"authorization_code"</c>, <c>"client_credentials"</c>.
///     </description>
///   </item>
///   <item>
///     <term>External-identity grant (validation → issuance bridge)</term>
///     <description>
///       The caller has ALREADY validated an external identity (e.g. Azure/Auth0 token
///       verified by an external token manager's <c>Validate</c>) and now asks FDW to mint
///       its own role/permission token for that subject.
///       Populate <see cref="ExternalPrincipal"/> with the <see cref="ClaimsPrincipal"/>
///       returned by the validation step; <see cref="Credential"/> is not used.
///       Recommended <see cref="GrantType"/> string: <c>"external_identity"</c>
///       (aligns with RFC 8693 token-exchange semantics; implementations may also
///       accept <c>"urn:ietf:params:oauth:grant-type:token-exchange"</c>).
///
///       The implementation resolves FDW roles and permissions from the subject
///       established by <see cref="ExternalPrincipal"/> and bakes them into the
///       issued FDW token — identical token shape to the credential path.
///     </description>
///   </item>
/// </list>
///
/// Both paths produce the same FDW role/permission token (FDW-resolved roles and
/// permissions baked in). The external-identity path delegates authN to an external
/// provider while FDW retains full ownership of authZ.
/// </summary>
public sealed class TokenIssuanceRequest
{
    /// <summary>
    /// Gets or sets the grant type string that selects the issuance path.
    /// See the class-level documentation for the two well-known paths.
    /// </summary>
    public string GrantType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subject identifier (username, agent key ID, authorization code, …)
    /// appropriate for the <see cref="GrantType"/>. Used on the credential path.
    /// On the external-identity path, the subject is taken from
    /// <see cref="ExternalPrincipal"/> instead.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the credential (password, agent key secret, PKCE verifier, …)
    /// appropriate for the <see cref="GrantType"/>. Never logged.
    /// Not used on the external-identity path.
    /// </summary>
    public string? Credential { get; set; }

    /// <summary>
    /// Gets or sets the already-validated external principal for the
    /// <b>external-identity grant path</b>.
    ///
    /// Set this to the <see cref="ClaimsPrincipal"/> returned by an external token
    /// manager's <c>Validate</c> (or an equivalent trusted source).
    /// The implementation extracts the subject identity and any relevant claims from
    /// this principal, resolves FDW roles/permissions, and mints the FDW token.
    ///
    /// Must be <c>null</c> on the credential path; must be non-null (and represent an
    /// authenticated identity) on the external-identity path.
    /// </summary>
    public ClaimsPrincipal? ExternalPrincipal { get; set; }

    /// <summary>
    /// Gets or sets the OAuth 2.0 scopes requested for the issued token.
    /// </summary>
    public IReadOnlyList<string> Scopes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the redirect URI, required for authorization-code grants.
    /// </summary>
    public string? RedirectUri { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier the caller wants to operate under.
    /// When null, the user's default tenant (UserTenants.IsDefault=1) is used.
    /// Mutually exclusive with <see cref="IsCrossTenant"/>: setting both is invalid.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the organization identifier within the tenant.
    /// When null and <see cref="TenantId"/> is set, the tenant's default org is used.
    /// </summary>
    public Guid? OrgId { get; set; }

    /// <summary>
    /// Gets or sets whether this is a cross-tenant token request.
    /// When true, the issued token carries the <c>cross_tenant</c> claim, allowing
    /// RLS to show rows across all tenants the user belongs to.
    /// Requires the user to hold the <c>tenants:view-all</c> permission.
    /// Mutually exclusive with <see cref="TenantId"/>: a cross-tenant token has no single active tenant.
    /// </summary>
    public bool IsCrossTenant { get; set; }
}
