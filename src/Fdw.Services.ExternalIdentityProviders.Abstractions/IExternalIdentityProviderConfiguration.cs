using Fdw.Configuration;
using Fdw.Services.ExternalIdentityProviders.Abstractions.Models;

namespace Fdw.Services.ExternalIdentityProviders.Abstractions;

/// <summary>
/// Marker interface for typed external-identity-provider body configurations (e.g. an OIDC-specific
/// configuration carrying Authority/ClientId/Audience/MetadataAddress). Each typed body
/// implements this interface directly without inheriting from a concrete header class — the header
/// (<c>ExternalIdentityProviderConfiguration</c>) carries a <c>[NotMapped]
/// IExternalIdentityProviderConfiguration? Configuration</c> property populated on the read path,
/// mirroring <c>ITokenManagerImplementationConfiguration</c>/<c>TokenManagerConfiguration</c>.
/// </summary>
public interface IExternalIdentityProviderConfiguration : IImplementationConfiguration
{
    /// <summary>
    /// Populates the public, non-secret fields of <paramref name="summary"/> that this typed body owns,
    /// for login-time provider discovery.
    /// </summary>
    /// <remarks>
    /// Why (FDW-624): the discovery endpoint previously down-cast to the concrete
    /// <c>OidcExternalIdentityProviderConfiguration</c> to read Authority/ClientId/MetadataAddress. That
    /// put a hard dependency on the Oidc option package into <c>Fdw.Web.Api</c> — so every API app
    /// dragged in external-IdP login whether or not it wanted it — and the null-conditional cast
    /// silently produced nulls for any non-Oidc provider instead of failing loud. Each typed body now
    /// projects its own public fields, so the endpoint never names a concrete option.
    /// <para>
    /// Implementations MUST write only non-secret values. The header row supplies Name/ProviderType;
    /// this method fills the option-specific remainder.
    /// </para>
    /// </remarks>
    /// <param name="summary">The summary DTO to populate.</param>
    void PopulateSummary(ExternalIdentityProviderSummaryDto summary);
}
