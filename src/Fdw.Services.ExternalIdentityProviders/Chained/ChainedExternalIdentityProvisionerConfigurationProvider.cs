using System;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.Chained;

/// <summary>
/// Typed-body configuration provider for <c>sec.ChainedExternalIdentityProvisioner</c> rows.
/// Extends <see cref="DefaultConfigurationProvider{TConfig,TCommand}"/> — all reads go to the gateway
/// against ConfigurationDb.
///
/// <c>Get(Guid id)</c> accepts the parent <c>sec.ExternalIdentityProvisioner.Id</c> (the durable
/// logical key) and routes to <c>WHERE [ExternalIdentityProvisionerId]=@p0 AND IsCurrent=1</c> via the
/// container FK key discovered from the IDataStore tree. Its ordered <c>Steps</c> child collection is
/// composed automatically by the base class's <c>ComposeChildren</c> cascade.
/// </summary>
/// <remarks>
/// Mirrors <c>OidcExternalIdentityProviderConfigurationProvider</c> from the ExternalIdentityProviders domain.
/// </remarks>
public class ChainedExternalIdentityProvisionerConfigurationProvider
    : DefaultConfigurationProvider<ChainedExternalIdentityProvisionerConfiguration, ChainedExternalIdentityProvisionerConfigurationCommand>
{
    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ChainedExternalIdentityProvisionerConfigurationProvider"/> class.
    /// </summary>
    public ChainedExternalIdentityProvisionerConfigurationProvider(
        ILogger<ChainedExternalIdentityProvisionerConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "sec")
        : base(logger ?? NullLogger<ChainedExternalIdentityProvisionerConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName)
    {
    }
}
