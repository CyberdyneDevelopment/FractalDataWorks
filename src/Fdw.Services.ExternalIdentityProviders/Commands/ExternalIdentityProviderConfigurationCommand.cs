using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

using Fdw.Services.ExternalIdentityProviders.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.Commands;

/// <summary>ConfigurationCommands TypeOption for the ExternalIdentityProvider configuration domain (auth.ExternalIdentityProvider).</summary>
[TypeOption(typeof(ConfigurationCommands), "ExternalIdentityProvider")]
public sealed class ExternalIdentityProviderConfigurationCommand : ConfigurationCommandBase<ExternalIdentityProviderConfiguration>
{
    /// <inheritdoc/>
    public ExternalIdentityProviderConfigurationCommand() : base("ExternalIdentityProvider") { }
}
