using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.ExternalIdentityProviders.Oidc;

/// <summary>
/// ConfigurationCommands TypeOption for the OidcExternalIdentityProvider typed-body domain.
/// Routes configuration save/delete operations for <see cref="OidcExternalIdentityProviderConfiguration"/>
/// (the Oidc typed-body record in <c>auth.OidcExternalIdentityProvider</c>).
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "OidcExternalIdentityProvider")]
public sealed class OidcExternalIdentityProviderConfigurationCommand
    : ConfigurationCommandBase<OidcExternalIdentityProviderConfiguration>
{
    /// <inheritdoc/>
    public OidcExternalIdentityProviderConfigurationCommand()
        : base("OidcExternalIdentityProvider") { }
}
