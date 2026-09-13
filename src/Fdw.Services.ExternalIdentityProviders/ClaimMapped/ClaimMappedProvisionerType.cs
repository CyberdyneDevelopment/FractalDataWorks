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

        Initialization((host, hostLoggerFactory) =>
        {
            var services = host.Services;

            var loggerFactory = services.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
            var logger = loggerFactory.CreateLogger<ClaimMappedProvisionerType>();

            services.GetRequiredService<IExternalIdentityProvisionerConfigurationProvider>().Register("ClaimMapped", services.GetRequiredService<IClaimMappedExternalIdentityProvisionerConfigurationProvider>());

            ExternalIdentityProvisionerLog.ProviderRegistered(logger, "ClaimMapped");

            return GenericResult<IHost>.Success(host);
        });
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Why this replaces what used to be here in <c>Initialization</c>: that callback resolved
    /// <see cref="IDomainServiceProvider{TService, TConfiguration}"/> from the ROOT container
    /// once, at startup, and this domain provider is registered <c>TryAddScoped</c> — so root's
    /// copy is one instance among many a real request never sees, and a real request's own
    /// instance never had this call reach it. This method is instead called once per
    /// construction, by <c>ExternalIdentityProvisionerTypes</c>'s own factory, and handed THAT
    /// construction's <paramref name="serviceProvider"/> — the same scope root or a request
    /// actually used to build <paramref name="domainProvider"/> itself.
    /// </remarks>
    public override IGenericResult RegisterImplementationProvider(IExternalIdentityProvisionerServiceProvider domainProvider, IServiceProvider serviceProvider, ILogger logger)
    {
        var factoryResult = domainProvider.Register(Name, () => serviceProvider.GetRequiredService<IClaimMappedProvisionerProvider>());
        if (!factoryResult.IsSuccess)
        {
            ServiceTypeLog.OptionFactoryRegistrationFailed(
                logger, nameof(ClaimMappedProvisionerType), Name, nameof(IClaimMappedProvisionerProvider), factoryResult.CurrentMessage);
            return factoryResult;
        }

        ServiceTypeLog.OptionFactoryRegistered(logger, nameof(ClaimMappedProvisionerType), Name, nameof(IClaimMappedProvisionerProvider));
        return factoryResult;
    }
}
