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
        Initialization((host, hostLoggerFactory) =>
        {
            var services = host.Services;
            var provider = services.GetRequiredService<IDomainServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>>();

            var loggerFactory = services.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
            var logger = loggerFactory.CreateLogger<ChainedExternalIdentityProvisionerType>();

            var implementationProvider = services.GetRequiredService<IChainedExternalIdentityProvisionerProvider>();

            services.GetRequiredService<IExternalIdentityProvisionerConfigurationProvider>().Register("Chained", services.GetRequiredService<IChainedExternalIdentityProvisionerConfigurationProvider>());


            var factoryResult = provider.Register("Chained", () => implementationProvider);
            if (!factoryResult.IsSuccess) return factoryResult.ToNewResult<IHost>();

            ServiceTypeLog.OptionFactoryRegistered(
                logger, nameof(ChainedExternalIdentityProvisionerType), Name, implementationProvider.GetType().Name);

            ExternalIdentityProvisionerLog.ProviderRegistered(logger, "Chained");

            return GenericResult<IHost>.Success(host);
        });

        Registration((builder, loggerFactory) =>
        {

            builder.Services.TryAddSingleton<ChainedExternalIdentityProvisionerConfigurationProvider>(sp => new ChainedExternalIdentityProvisionerConfigurationProvider(sp.GetRequiredService<ILogger<ChainedExternalIdentityProvisionerConfigurationProvider>>(), sp.GetRequiredService<IConfigurationGatewayProvider>(), ExternalIdentityProvisionerTypes.ConfigurationConnection));
            builder.Services.TryAddSingleton<IChainedExternalIdentityProvisionerConfigurationProvider>(sp => sp.GetRequiredService<ChainedExternalIdentityProvisionerConfigurationProvider>());

            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IChainedExternalIdentityProvisionerFactory), typeof(ChainedExternalIdentityProvisionerFactory), ServiceLifetime.Scoped));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IChainedExternalIdentityProvisionerProvider), typeof(ChainedExternalIdentityProvisionerProvider), ServiceLifetime.Scoped));
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
