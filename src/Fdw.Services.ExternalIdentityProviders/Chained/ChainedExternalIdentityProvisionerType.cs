using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.Services;
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

            // Decorate the domain's own AddScoped<IDomainServiceProvider<...>> registration --
            // already in builder.Services by the time this runs, since ExternalIdentityProvisionerTypes'
            // own Registration body sets it up before running the option loop.
            var existing = builder.Services.Single(d =>
                d.ServiceType == typeof(IDomainServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>));
            builder.Services.Remove(existing);
            builder.Services.AddScoped<IDomainServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>>(sp =>
            {
                var provider = (IDomainServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>)existing.ImplementationFactory!(sp);

                var factoryResult = provider.Register(Name, () => sp.GetRequiredService<IChainedExternalIdentityProvisionerProvider>());
                if (!factoryResult.IsSuccess)
                {
                    ServiceTypeLog.OptionFactoryRegistrationFailed(
                        sp.GetService<ILoggerFactory>()?.CreateLogger<ChainedExternalIdentityProvisionerType>() ?? NullLogger<ChainedExternalIdentityProvisionerType>.Instance,
                        nameof(ChainedExternalIdentityProvisionerType), Name, nameof(IChainedExternalIdentityProvisionerProvider), factoryResult.CurrentMessage);
                }

                return provider;
            });

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // IExternalIdentityProvisionerConfigurationProvider is Singleton, so root's Initialize call
        // reaches the same instance every later resolution sees -- unlike the domain SERVICE
        // provider above, this one is safe to populate once, here, unmodified.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            services.GetRequiredService<IExternalIdentityProvisionerConfigurationProvider>()
                .Register(Name, services.GetRequiredService<IChainedExternalIdentityProvisionerConfigurationProvider>());
            ExternalIdentityProvisionerLog.ProviderRegistered(
                loggerFactory?.CreateLogger<ChainedExternalIdentityProvisionerType>() ?? NullLogger<ChainedExternalIdentityProvisionerType>.Instance, Name);
            return GenericResult<IHost>.Success(host);
        });
    }
}
