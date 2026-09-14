using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Results;
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
/// instances. Mirrors <see cref="Chained.ChainedExternalIdentityProvisionerType"/> structurally.
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
                domainConfigurationProvider.Register(Name, serviceProvider.GetRequiredService<IClaimMappedExternalIdentityProvisionerConfigurationProvider>());
                ExternalIdentityProvisionerLog.ProviderRegistered(logger, Name);
            }

            if (domainProvider is null)
                return GenericResult.Success();

            var factoryResult = domainProvider.Register(Name, () => serviceProvider.GetRequiredService<IClaimMappedProvisionerProvider>());
            if (!factoryResult.IsSuccess)
            {
                ServiceTypeLog.OptionFactoryRegistrationFailed(
                    logger, nameof(ClaimMappedProvisionerType), Name, nameof(IClaimMappedProvisionerProvider), factoryResult.CurrentMessage);
                return factoryResult;
            }

            ServiceTypeLog.OptionFactoryRegistered(logger, nameof(ClaimMappedProvisionerType), Name, nameof(IClaimMappedProvisionerProvider));
            return factoryResult;
        });
    }
}
