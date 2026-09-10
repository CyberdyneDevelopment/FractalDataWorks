using System;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Fdw.Services.ExternalIdentityProviders.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders;

/// <summary>
/// Configuration provider for ExternalIdentityProvisionerConfiguration rows in
/// sec.ExternalIdentityProvisioner. Reads through IConfigurationGateway — no IConfiguration binding
/// section.
/// </summary>
public class ExternalIdentityProvisionerConfigurationProvider
    : ImplementationConfigurationProviderBase<ExternalIdentityProvisionerConfiguration, IExternalIdentityProvisionerImplementationConfiguration, ExternalIdentityProvisionerConfigurationCommand>,
      IExternalIdentityProvisionerConfigurationProvider
{
    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;


    /// <summary>Initializes a new instance of the <see cref="ExternalIdentityProvisionerConfigurationProvider"/> class.</summary>
    public ExternalIdentityProvisionerConfigurationProvider(
        ILogger<ExternalIdentityProvisionerConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "sec")
        : base(logger ?? NullLogger<ExternalIdentityProvisionerConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
    }
}
