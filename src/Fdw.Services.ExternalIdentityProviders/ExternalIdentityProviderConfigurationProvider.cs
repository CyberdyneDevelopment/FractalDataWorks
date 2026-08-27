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
/// Configuration provider for ExternalIdentityProviderConfiguration rows in auth.ExternalIdentityProvider.
/// Reads through IConfigurationGateway — no IConfiguration binding section.
/// </summary>
// Why: ExternalIdentityProviderConfiguration is loaded from ConfigurationDb at runtime via
// Lazy<IConfigurationGateway>, not through BindConfiguration("ExternalIdentityProviders:..."). Mirrors
// TokenManagerConfigurationProvider exactly.
public class ExternalIdentityProviderConfigurationProvider
    : ServiceConfigurationProviderBase<
          ExternalIdentityProviderConfiguration,
          IExternalIdentityProviderImplementationConfiguration,
          ExternalIdentityProviderConfigurationCommand>,
      IExternalIdentityProviderConfigurationProvider
{

    /// <summary>Initializes a new instance of the <see cref="ExternalIdentityProviderConfigurationProvider"/> class.</summary>
    public ExternalIdentityProviderConfigurationProvider(
        ILogger<ExternalIdentityProviderConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "auth")
        : base(logger ?? NullLogger<ExternalIdentityProviderConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
    }

    /// <inheritdoc />
    protected override ExternalIdentityProviderConfiguration Compose<T>(
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
