using System;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Fdw.Services.ExternalIdentityProviders.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.Chained;

/// <summary>
/// Typed-body configuration provider for <c>sec.ChainedExternalIdentityProvisioner</c> rows.
/// Extends <see cref="ImplementationConfigurationProviderBase{TConfig,TCommand}"/> — all reads go to the gatewayProvider
/// against ConfigurationDb.
///
/// <c>Get(Guid id)</c> accepts the parent <c>sec.ExternalIdentityProvisioner.Id</c> (the durable
/// logical key) and routes to <c>WHERE [ExternalIdentityProvisionerId]=@p0 AND IsCurrent=1</c> via the
/// container FK key discovered from the IDataStore tree. Its ordered <c>Steps</c> child collection is
/// composed automatically by the base class's <c>ComposeChildren</c> cascade.
/// </summary>
/// <remarks>
/// Same shape as any other typed-body configuration provider in this domain — reads through
/// <c>IConfigurationGateway</c>, no <c>IConfiguration</c> binding section.
/// </remarks>
public class ChainedExternalIdentityProvisionerConfigurationProvider
    : ImplementationConfigurationProvider<
          IExternalIdentityProvisionerImplementationConfiguration,
          ChainedExternalIdentityProvisionerConfiguration,
          ChainedExternalIdentityProvisionerConfigurationCommand>
{
    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ChainedExternalIdentityProvisionerConfigurationProvider"/> class.
    /// </summary>
    public ChainedExternalIdentityProvisionerConfigurationProvider(
        ILogger<ChainedExternalIdentityProvisionerConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "sec")
        : base(logger ?? NullLogger<ChainedExternalIdentityProvisionerConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
    }
}
