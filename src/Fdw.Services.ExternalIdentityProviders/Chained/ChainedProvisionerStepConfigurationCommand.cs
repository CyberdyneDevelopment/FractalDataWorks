using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

using Fdw.Services.ExternalIdentityProviders.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.Chained;

/// <summary>
/// ConfigurationCommands TypeOption for the ChainedProvisionerStep child domain. Routes save/delete
/// operations for <see cref="ChainedProvisionerStepConfiguration"/> (ordered child rows in
/// <c>sec.ChainedProvisionerStep</c>). Registered so
/// <c>ImplementationConfigurationProviderBase.CascadeCollections</c>/<c>ComposeChildren</c> can locate the table
/// for the <see cref="ChainedExternalIdentityProvisionerConfiguration.Steps"/> cascade.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "ChainedProvisionerStep")]
public sealed class ChainedProvisionerStepConfigurationCommand
    : ConfigurationCommandBase<ChainedProvisionerStepConfiguration>
{
    /// <inheritdoc/>
    public ChainedProvisionerStepConfigurationCommand()
        : base("ChainedProvisionerStep") { }
}
