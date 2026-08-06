using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Authentication.OpenIddict;

/// <summary>
/// ConfigurationCommands TypeOption for the OpenIddictTokenManager typed-body domain.
/// Routes configuration save/delete operations for <see cref="OpenIddictTokenManagerConfiguration"/>
/// (the OpenIddict typed-body record in <c>auth.OpenIddictTokenManager</c>).
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "OpenIddictTokenManager")]
public sealed class OpenIddictTokenManagerConfigurationCommand
    : ConfigurationCommandBase<OpenIddictTokenManagerConfiguration>
{
    /// <inheritdoc/>
    public OpenIddictTokenManagerConfigurationCommand()
        : base("OpenIddictTokenManager") { }
}
