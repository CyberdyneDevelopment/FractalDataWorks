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
/// Chained <see cref="ExternalIdentityProvisionerTypes"/> ServiceTypeOption. Registers the header +
/// typed-body gateway-backed configuration providers and the
/// <see cref="ChainedExternalIdentityProvisionerFactory"/> that builds
/// <see cref="ChainedExternalIdentityProvisioner"/> instances.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(ExternalIdentityProvisionerTypes), "Chained")]
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
            var provider = services.GetRequiredService<IPlatformServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>>();

            var loggerFactory = services.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
            var logger = loggerFactory.CreateLogger<ChainedExternalIdentityProvisionerType>();

            var factory = services.GetRequiredService<IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>>();
            var domainProvider = services.GetRequiredService<IExternalIdentityProvisionerConfigurationProvider>();
            var typedProvider = services.GetRequiredService<ChainedExternalIdentityProvisionerConfigurationProvider>();

            domainProvider.Register("Chained", typedProvider);


            var factoryResult = provider.Register("Chained", factory);
            if (!factoryResult.IsSuccess) return factoryResult.ToNewResult<IHost>();

            ServiceTypeLog.OptionFactoryRegistered(
                logger, nameof(ChainedExternalIdentityProvisionerType), Name, factory.GetType().Name);

            ExternalIdentityProvisionerLog.ProviderRegistered(logger, "Chained");

            return GenericResult<IHost>.Success(host);
        });

        Registration((builder, loggerFactory) =>
        {

            builder.Services.TryAddSingleton<ChainedExternalIdentityProvisionerConfigurationProvider>(sp =>
                new ChainedExternalIdentityProvisionerConfigurationProvider(
                    sp.GetService<ILogger<ChainedExternalIdentityProvisionerConfigurationProvider>>()!,
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStore,
                    PathName));

            builder.Services.TryAddScoped<ChainedExternalIdentityProvisionerFactory>();
            builder.Services.TryAddScoped<IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>>(
                sp => sp.GetRequiredService<ChainedExternalIdentityProvisionerFactory>());
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
