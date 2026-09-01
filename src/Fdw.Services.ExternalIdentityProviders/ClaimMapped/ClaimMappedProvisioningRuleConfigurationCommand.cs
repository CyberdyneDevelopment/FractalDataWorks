using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.ExternalIdentityProviders.ClaimMapped;

/// <summary>
/// ConfigurationCommands TypeOption for the ClaimMappedProvisioningRule child domain. Routes
/// save/delete operations for <see cref="ClaimMappedProvisioningRuleConfiguration"/> (ordered child
/// rows in <c>sec.ClaimMappedProvisioningRule</c>). Registered so
/// <c>ImplementationConfigurationProviderBase.CascadeCollections</c>/<c>ComposeChildren</c> can locate
/// the table for the <see cref="ClaimMappedExternalIdentityProvisionerConfiguration.Rules"/> cascade.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "ClaimMappedProvisioningRule")]
public sealed class ClaimMappedProvisioningRuleConfigurationCommand
    : ConfigurationCommandBase<ClaimMappedProvisioningRuleConfiguration>
{
    /// <inheritdoc/>
    public ClaimMappedProvisioningRuleConfigurationCommand()
        : base("ClaimMappedProvisioningRule") { }
}
