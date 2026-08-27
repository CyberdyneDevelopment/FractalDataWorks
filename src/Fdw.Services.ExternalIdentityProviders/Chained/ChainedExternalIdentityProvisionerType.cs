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
        // Why Initialize and not Register: this wiring needs a LIVE container (it resolves the
        // domain provider and its typed-body providers), and Register runs while the container
        // is still being built. Initialize runs after Build() with a real IServiceProvider.
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

            // Why Trace here when ProviderRegistered already reports this option at Information: that
            // line names the option and nothing else. This one names the type that did the registering
            // and the factory it registered, which is what distinguishes a base wiring an option from
            // the option wiring itself — and no failure line is needed alongside it, because unlike the
            // other options in this domain these three results propagate and the collect reports them.
            ServiceTypeLog.OptionFactoryRegistered(
                logger, nameof(ChainedExternalIdentityProvisionerType), Name, factory.GetType().Name);

            ExternalIdentityProvisionerLog.ProviderRegistered(logger, "Chained");

            return GenericResult<IHost>.Success(host);
        });

        Registration((builder, loggerFactory) =>
        {

            // Why: registers the header (ExternalIdentityProvisionerConfigurationProvider) config provider
            // this option depends on — idempotent (TryAdd*), safe for every sibling option to call.
            ExternalIdentityProvisionerConfigurationProvider.RegisterDomainServices(builder.Services);

            builder.Services.TryAddSingleton<ChainedExternalIdentityProvisionerConfigurationProvider>(sp =>
                new ChainedExternalIdentityProvisionerConfigurationProvider(
                    sp.GetService<ILogger<ChainedExternalIdentityProvisionerConfigurationProvider>>()!,
                    sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                    DataStore,
                    PathName));

            // Why: the factory is a PURE constructor (logger only) — it holds no providers and resolves
            // nothing. The provisioner provider it needs for Provision-time sibling lookup is passed in by
            // DefaultExternalIdentityProvisionerProvider (as `this`) at Create time. That is what keeps
            // resolving this factory from re-entering the provider's own resolver lambda (FDW-615).
            // Scoped to match the provider's generated lifetime — never Singleton over a Scoped dependency.
            builder.Services.TryAddScoped<ChainedExternalIdentityProvisionerFactory>();
            builder.Services.TryAddScoped<IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>>(
                sp => sp.GetRequiredService<ChainedExternalIdentityProvisionerFactory>());
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
