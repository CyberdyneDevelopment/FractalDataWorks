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

            var loggerFactory = services.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
            var logger = loggerFactory.CreateLogger<ChainedExternalIdentityProvisionerType>();

            services.GetRequiredService<IExternalIdentityProvisionerConfigurationProvider>().Register("Chained", services.GetRequiredService<IChainedExternalIdentityProvisionerConfigurationProvider>());

            ExternalIdentityProvisionerLog.ProviderRegistered(logger, "Chained");

            return GenericResult<IHost>.Success(host);
        });

        Registration((builder, loggerFactory) =>
        {

            builder.Services.AddSingleton<IChainedExternalIdentityProvisionerConfigurationProvider, ChainedExternalIdentityProvisionerConfigurationProvider>(sp => new ChainedExternalIdentityProvisionerConfigurationProvider(sp.GetRequiredService<ILogger<ChainedExternalIdentityProvisionerConfigurationProvider>>(), sp.GetRequiredService<IConfigurationGatewayProvider>(), ExternalIdentityProvisionerTypes.ConfigurationConnection));

            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IChainedExternalIdentityProvisionerFactory), typeof(ChainedExternalIdentityProvisionerFactory), ServiceLifetime.Scoped));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IChainedExternalIdentityProvisionerProvider), typeof(ChainedExternalIdentityProvisionerProvider), ServiceLifetime.Scoped));
            return GenericResult<IHostApplicationBuilder>.Success(builder);
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
        var factoryResult = domainProvider.Register(Name, () => serviceProvider.GetRequiredService<IChainedExternalIdentityProvisionerProvider>());
        if (!factoryResult.IsSuccess)
        {
            ServiceTypeLog.OptionFactoryRegistrationFailed(
                logger, nameof(ChainedExternalIdentityProvisionerType), Name, nameof(IChainedExternalIdentityProvisionerProvider), factoryResult.CurrentMessage);
            return factoryResult;
        }

        ServiceTypeLog.OptionFactoryRegistered(logger, nameof(ChainedExternalIdentityProvisionerType), Name, nameof(IChainedExternalIdentityProvisionerProvider));
        return factoryResult;
    }
}
