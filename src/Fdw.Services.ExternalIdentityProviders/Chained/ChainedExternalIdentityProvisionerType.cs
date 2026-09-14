using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Logging;
using Fdw.ServiceTypes;
using Fdw.ServiceTypes.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Results;

using Fdw.Configuration;

namespace Fdw.Services.ExternalIdentityProviders.Chained;

/// <summary>
/// Chained <see cref="ExternalIdentityProvisionerTypes"/> Implementation. Registers the header +
/// typed-body gateway-backed configuration providers and the
/// <see cref="ChainedExternalIdentityProvisionerFactory"/> that builds
/// <see cref="ChainedExternalIdentityProvisioner"/> instances.
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(ExternalIdentityProvisionerTypes), "Chained")]
public sealed class ChainedExternalIdentityProvisionerType
    : ExternalIdentityProvisionerTypeBase<
        IExternalIdentityProvisioner,
        IExternalIdentityProvisionerImplementationConfiguration,
        IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>>
{
    /// <summary>Initializes a new instance of <see cref="ChainedExternalIdentityProvisionerType"/>.</summary>
    public ChainedExternalIdentityProvisionerType() : base(name: "Chained", defaultContainerName: "ExternalIdentityProvisioner")
    {
        Registration((builder, loggerFactory) =>
        {

            builder.Services.AddSingleton<IChainedExternalIdentityProvisionerConfigurationProvider, ChainedExternalIdentityProvisionerConfigurationProvider>(sp => new ChainedExternalIdentityProvisionerConfigurationProvider(sp.GetRequiredService<ILogger<ChainedExternalIdentityProvisionerConfigurationProvider>>(), sp.GetRequiredService<IConfigurationGatewayProvider>(), ExternalIdentityProvisionerTypes.ConfigurationConnection));

            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IChainedExternalIdentityProvisionerFactory), typeof(ChainedExternalIdentityProvisionerFactory), ServiceLifetime.Scoped));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IChainedExternalIdentityProvisionerProvider), typeof(ChainedExternalIdentityProvisionerProvider), ServiceLifetime.Scoped));
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // Called once per ExternalIdentityProvisionerTypes AddScoped construction, with THAT
        // construction's own serviceProvider — so whichever scope actually builds domainProvider
        // (root at startup, a real request's own scope for a request) is the same scope this
        // closure resolves against.
        Registration((serviceProvider, domainProvider, domainConfigurationProvider, logger) =>
        {
            if (domainConfigurationProvider is not null)
            {
                domainConfigurationProvider.Register(Name, serviceProvider.GetRequiredService<IChainedExternalIdentityProvisionerConfigurationProvider>());
                ExternalIdentityProvisionerLog.ProviderRegistered(logger, Name);
            }

            if (domainProvider is null)
                return GenericResult.Success();

            var factoryResult = domainProvider.Register(Name, () => serviceProvider.GetRequiredService<IChainedExternalIdentityProvisionerProvider>());
            if (!factoryResult.IsSuccess)
            {
                ServiceTypeLog.OptionFactoryRegistrationFailed(
                    logger, nameof(ChainedExternalIdentityProvisionerType), Name, nameof(IChainedExternalIdentityProvisionerProvider), factoryResult.CurrentMessage);
                return factoryResult;
            }

            ServiceTypeLog.OptionFactoryRegistered(logger, nameof(ChainedExternalIdentityProvisionerType), Name, nameof(IChainedExternalIdentityProvisionerProvider));
            return factoryResult;
        });
    }
}
