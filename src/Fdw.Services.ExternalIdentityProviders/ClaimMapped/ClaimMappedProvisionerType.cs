using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.Services.Authorization;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Binding;
using Fdw.Services.ExternalIdentityProviders.Logging;
using Fdw.Services.Users;
using Fdw.ServiceTypes;
using Fdw.ServiceTypes.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.ClaimMapped;

/// <summary>
/// ClaimMapped <see cref="ExternalIdentityProvisionerTypes"/> Implementation. Registers the header
/// + typed-body gateway-backed configuration providers and the
/// <see cref="ClaimMappedProvisionerFactory"/> that builds <see cref="ClaimMappedProvisioner"/>
/// instances.
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(ExternalIdentityProvisionerTypes), "ClaimMapped")]
public sealed class ClaimMappedProvisionerType
    : ExternalIdentityProvisionerTypeBase<
        IExternalIdentityProvisioner,
        IExternalIdentityProvisionerImplementationConfiguration,
        IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>>
{
    /// <summary>Initializes a new instance of <see cref="ClaimMappedProvisionerType"/>.</summary>
    public ClaimMappedProvisionerType() : base(name: "ClaimMapped", defaultContainerName: "ExternalIdentityProvisioner")
    {
        Registration((builder, loggerFactory) =>
        {
            builder.Services.AddSingleton<IClaimMappedExternalIdentityProvisionerConfigurationProvider, ClaimMappedExternalIdentityProvisionerConfigurationProvider>(sp => new ClaimMappedExternalIdentityProvisionerConfigurationProvider(sp.GetRequiredService<ILogger<ClaimMappedExternalIdentityProvisionerConfigurationProvider>>(), sp.GetRequiredService<IConfigurationGatewayProvider>(), ExternalIdentityProvisionerTypes.ConfigurationConnection));

            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IClaimMappedProvisionerFactory), typeof(ClaimMappedProvisionerFactory), ServiceLifetime.Scoped));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IClaimMappedProvisionerProvider), typeof(ClaimMappedProvisionerProvider), ServiceLifetime.Scoped));

            // Decorate the domain's own AddScoped<IDomainServiceProvider<...>> registration --
            // already in builder.Services by the time this runs, since ExternalIdentityProvisionerTypes'
            // own Registration body sets it up before running the option loop.
            var existing = builder.Services.Single(d =>
                d.ServiceType == typeof(IDomainServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>));
            builder.Services.Remove(existing);
            builder.Services.AddScoped<IDomainServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>>(sp =>
            {
                var provider = (IDomainServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>)existing.ImplementationFactory!(sp);

                var factoryResult = provider.Register(Name, () => sp.GetRequiredService<IClaimMappedProvisionerProvider>());
                if (!factoryResult.IsSuccess)
                {
                    ServiceTypeLog.OptionFactoryRegistrationFailed(
                        sp.GetService<ILoggerFactory>()?.CreateLogger<ClaimMappedProvisionerType>() ?? NullLogger<ClaimMappedProvisionerType>.Instance,
                        nameof(ClaimMappedProvisionerType), Name, nameof(IClaimMappedProvisionerProvider), factoryResult.CurrentMessage);
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
                .Register(Name, services.GetRequiredService<IClaimMappedExternalIdentityProvisionerConfigurationProvider>());
            ExternalIdentityProvisionerLog.ProviderRegistered(
                loggerFactory?.CreateLogger<ClaimMappedProvisionerType>() ?? NullLogger<ClaimMappedProvisionerType>.Instance, Name);
            return GenericResult<IHost>.Success(host);
        });
    }
}
