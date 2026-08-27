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
// Why: ExternalIdentityProvisionerConfiguration is loaded from ConfigurationDb at runtime via
// Lazy<IConfigurationGateway>, not through BindConfiguration("ExternalIdentityProvisioners:..."). Mirrors
// ExternalIdentityProviderConfigurationProvider exactly, targeting sec instead of auth.
public class ExternalIdentityProvisionerConfigurationProvider
    : ServiceConfigurationProviderBase<
          ExternalIdentityProvisionerConfiguration,
          IExternalIdentityProvisionerImplementationConfiguration,
          ExternalIdentityProvisionerConfigurationCommand>,
      IExternalIdentityProvisionerConfigurationProvider
{

    /// <summary>Initializes a new instance of the <see cref="ExternalIdentityProvisionerConfigurationProvider"/> class.</summary>
    public ExternalIdentityProvisionerConfigurationProvider(
        ILogger<ExternalIdentityProvisionerConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "sec")
        : base(logger ?? NullLogger<ExternalIdentityProvisionerConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName)
    {
    }

    /// <inheritdoc />
    protected override ExternalIdentityProvisionerConfiguration Compose<T>(
        string serviceOptionType,
        string name,
        T implementationConfiguration)
        => new()
        {
            Name = name,
            ServiceOptionType = serviceOptionType,
            Configuration = implementationConfiguration,
        };
}
