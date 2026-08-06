using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Logging;
using Fdw.ServiceTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
        ExternalIdentityProvisionerConfiguration,
        IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration>>
{
    /// <summary>Initializes a new instance of <see cref="ChainedExternalIdentityProvisionerType"/>.</summary>
    public ChainedExternalIdentityProvisionerType() : base(name: "Chained", defaultContainerName: "ExternalIdentityProvisioner")
    {
        // Why Initialize and not Register: this wiring needs a LIVE container (it resolves the
        // domain provider and its typed-body providers), and Register runs while the container
        // is still being built. Initialize runs after Build() with a real IServiceProvider.
        Initialization((services, hostLoggerFactory) =>
        {
            var provider = services.GetRequiredService<IFdwServiceProvider<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration>>();

            var loggerFactory = services.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
            var logger = loggerFactory.CreateLogger<ChainedExternalIdentityProvisionerType>();

            var factory = services.GetRequiredService<IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration>>();
            var headerProvider = services.GetRequiredService<ExternalIdentityProvisionerConfigurationProvider>();
            var typedProvider = services.GetRequiredService<ChainedExternalIdentityProvisionerConfigurationProvider>();

            // Why: register the Chained typed-body provider with the header provider so ComposeTypedBody
            // dispatches to sec.ChainedExternalIdentityProvisioner rows when the discriminator is "Chained".
            headerProvider.RegisterTypedProvider("Chained", typedProvider);

            // Why: multiple ExternalIdentityProvisionerTypes options may register against the SAME header
            // provider — RegisterParentProvider is safe to call from every option since they all point at
            // the one sec.ExternalIdentityProvisioner table.
            var parentResult = provider.RegisterParentProvider(headerProvider);
            if (!parentResult.IsSuccess) return services;

            var factoryResult = provider.Register("Chained", factory);
            if (!factoryResult.IsSuccess) return services;

            var headerResult = provider.Register("Chained", headerProvider);
            if (!headerResult.IsSuccess) return services;

            ExternalIdentityProvisionerLog.ProviderRegistered(logger, "Chained");
    
            return services;
        });

        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {

            // Why: registers the header (ExternalIdentityProvisionerConfigurationProvider) config provider
            // this option depends on — idempotent (TryAdd*), safe for every sibling option to call.
            ExternalIdentityProvisionerConfigurationProvider.RegisterDomainServices(builder.Services);

            builder.Services.TryAddSingleton<ChainedExternalIdentityProvisionerConfigurationProvider>(sp =>
                new ChainedExternalIdentityProvisionerConfigurationProvider(
                    sp.GetService<ILogger<ChainedExternalIdentityProvisionerConfigurationProvider>>()!,
                    sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                    dataStoreName,
                    pathName,
                    new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));

            // Why: the factory is a PURE constructor (logger only) — it holds no providers and resolves
            // nothing. The provisioner provider it needs for Provision-time sibling lookup is passed in by
            // DefaultExternalIdentityProvisionerProvider (as `this`) at Create time. That is what keeps
            // resolving this factory from re-entering the provider's own resolver lambda (FDW-615).
            // Scoped to match the provider's generated lifetime — never Singleton over a Scoped dependency.
            builder.Services.TryAddScoped<ChainedExternalIdentityProvisionerFactory>();
            builder.Services.TryAddScoped<IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration>>(
                sp => sp.GetRequiredService<ChainedExternalIdentityProvisionerFactory>());
            return builder;
        });

    }

}
