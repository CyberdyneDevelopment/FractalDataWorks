using Fdw.Configuration;

namespace Fdw.Services.ExternalIdentityProviders.Abstractions;

/// <summary>
/// Marker interface for typed external-identity-provisioner body configurations (e.g. the Chained
/// provisioner's typed body, which carries no scalar columns of its own — its policy lives entirely
/// in its ordered <c>ChainedProvisionerStep</c> children). Each typed body implements this interface
/// directly without inheriting from a concrete header class — the header
/// (<c>ExternalIdentityProvisionerConfiguration</c>) carries a <c>[NotMapped]
/// IExternalIdentityProvisionerImplementationConfiguration? Configuration</c> property populated on the read path,
/// the domain header for a <c>sec.ExternalIdentityProvisioner</c> row's typed body.
/// </summary>
public interface IExternalIdentityProvisionerImplementationConfiguration : IImplementationConfiguration
{
}
