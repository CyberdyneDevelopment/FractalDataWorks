using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.ServiceTypes.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;
using Fdw.Results;
using Microsoft.Extensions.Hosting;

using Fdw.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Commands;
using Fdw.Services.ExternalIdentityProviders.Binding;

namespace Fdw.Services.ExternalIdentityProviders;

/// <summary>
/// Collection of external identity provisioner service types. Collected by PlatformServices like
/// every other domain — no host registers it by hand. Config-selected: a (tenant, external provider)
/// pair binds to
/// exactly one active <c>sec.ExternalIdentityProvisioner</c> row via
/// <c>ExternalIdentityProvisionerBindingConfigurationProvider</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(ExternalIdentityProvisionerTypeBase<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration, IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>>),
    typeof(IExternalIdentityProvisionerType),
    typeof(ExternalIdentityProvisionerTypes),
    ServiceInterface = typeof(IExternalIdentityProvisioner),
    ProviderType = typeof(ExternalIdentityProvisionerServiceProvider),
    ProviderInterface = typeof(IPlatformServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>),
    ServiceCategory = "ExternalIdentityProvisioner")]
public partial class ExternalIdentityProvisionerTypes : ServiceTypeCollectionBase<
    ExternalIdentityProvisionerTypeBase<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration, IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>>,
    IExternalIdentityProvisionerType>
{
    /// <summary>
    /// The connection this domain's configuration rows are read from and written to.
    /// </summary>
    public static string ConfigurationConnection { get; set; } = "PlatformConfiguration";

    // Configure(), Register(), Initialize() are source-generated.

    /// <summary>
    /// Sets this collection's Register body: the option collect, then this domain's provider.
    /// </summary>
    /// <remarks>
    /// The provider is one registration for the whole collection and this declaration already names it,
    /// so the body that registers it is written here beside the declaration. Setting it as the phase's
    /// body is what makes it replaceable: an application calling <c>Registration(...)</c> replaces the
    /// collect and this registration together, which is the correct semantic for a host taking over phase 2.
    /// </remarks>
    static ExternalIdentityProvisionerTypes()
    {
        var collectOptions = RegisterFunc;

        var providerService = typeof(IPlatformServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>).ToString();

        Registration((builder, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<ExternalIdentityProvisionerTypes>() ?? NullLogger<ExternalIdentityProvisionerTypes>.Instance;

            var registered = collectOptions(builder, loggerFactory);
            if (registered.IsFailure)
                return registered;

            var declaredOptions = Options;
            var optionNames = string.Join(", ", declaredOptions.Select(option => option.Name));

            ServiceTypeLog.DomainOptionsCollected(log, nameof(ExternalIdentityProvisionerTypes), declaredOptions.Length, optionNames);
            ServiceTypeLog.DomainProviderDeclared(log, nameof(ExternalIdentityProvisionerTypes), providerService);

            builder.Services.TryAddSingleton<ExternalIdentityProvisionerBindingConfigurationProvider>(sp =>
                new ExternalIdentityProvisionerBindingConfigurationProvider(
                    sp.GetService<ILogger<ExternalIdentityProvisionerBindingConfigurationProvider>>()!,
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                        ExternalIdentityProvisionerTypes.ConfigurationConnection));

            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<ExternalIdentityProvisionerBindingConfiguration, ExternalIdentityProvisionerBindingConfigurationCommand>>(
                sp => sp.GetRequiredService<ExternalIdentityProvisionerBindingConfigurationProvider>());

            builder.Services.TryAddSingleton<IServiceConfigurationProvider<ExternalIdentityProvisionerBindingConfiguration>>(sp =>
                sp.GetRequiredService<ExternalIdentityProvisionerBindingConfigurationProvider>());

            builder.Services.TryAddSingleton<IExternalIdentityProvisionerConfigurationProvider>(sp =>
                new ExternalIdentityProvisionerConfigurationProvider(
                    sp.GetService<ILogger<ExternalIdentityProvisionerConfigurationProvider>>()!,
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    ConfigurationConnection));
            builder.Services.TryAddSingleton<ExternalIdentityProvisionerConfigurationProvider>(
                sp => (ExternalIdentityProvisionerConfigurationProvider)sp.GetRequiredService<IExternalIdentityProvisionerConfigurationProvider>());
            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<ExternalIdentityProvisionerConfiguration, ExternalIdentityProvisionerConfigurationCommand>>(
                sp => sp.GetRequiredService<ExternalIdentityProvisionerConfigurationProvider>());
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<ExternalIdentityProvisionerConfiguration>>(
                sp => sp.GetRequiredService<ExternalIdentityProvisionerConfigurationProvider>());

            builder.Services.AddScoped<IPlatformServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>>(sp =>
            {
                var provider = new ExternalIdentityProvisionerServiceProvider(
                    sp,
                    sp.GetService<ILoggerFactory>()?.CreateLogger<ExternalIdentityProvisionerServiceProvider>()
                    ?? NullLogger<ExternalIdentityProvisionerServiceProvider>.Instance);

                var stLogger = sp.GetService<ILoggerFactory>()?.CreateLogger<ExternalIdentityProvisionerTypes>()
                    ?? NullLogger<ExternalIdentityProvisionerTypes>.Instance;
                ServiceTypeLog.DomainProviderConstructing(stLogger, nameof(ExternalIdentityProvisionerTypes), provider.GetType().Name);
                try
                {
                    if (sp.GetService<IExternalIdentityProvisionerConfigurationProvider>() is { } cfgProvider)
                    {
                        var domainResult = provider.Register(cfgProvider);
                        if (domainResult.IsSuccess)
                            ServiceTypeLog.DomainConfigurationSourceAttached(stLogger, nameof(ExternalIdentityProvisionerTypes), provider.GetType().Name, cfgProvider.GetType().Name);
                        else
                            ServiceTypeLog.DomainConfigurationSourceRejected(stLogger, nameof(ExternalIdentityProvisionerTypes), provider.GetType().Name, cfgProvider.GetType().Name, domainResult.CurrentMessage);
                    }
                    else
                    {
                        ServiceTypeLog.DomainHasNoConfigurationSource(
                            stLogger,
                            nameof(ExternalIdentityProvisionerTypes),
                            provider.GetType().Name,
                            typeof(IServiceConfigurationProvider<IExternalIdentityProvisionerImplementationConfiguration>).ToString());
                    }
                }
                catch (Exception ex)
                {
                    ServiceTypeLog.FactoryRegistrationException(stLogger, ex, nameof(ExternalIdentityProvisionerTypes));
                    throw;
                }
                return provider;
            });

            // Published under the domain-named interface as well as the closed generic: a caller
            // asking for IExternalIdentityProvisionerServiceProvider states which domain it needs,
            // rather than a shape another IPlatformServiceProvider<TService, TConfig> could also
            // satisfy — and it has to be the SAME instance the closed generic resolves, or a caller
            // reached through this name would register against a provider whose factories the one
            // reached through the closed generic never sees.
            builder.Services.TryAddScoped<IExternalIdentityProvisionerServiceProvider>(sp =>
                (IExternalIdentityProvisionerServiceProvider)sp.GetRequiredService<
                    IPlatformServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>>());

            if (declaredOptions.Length == 0)
                ServiceTypeLog.DomainRegisteredWithNoOptions(log, nameof(ExternalIdentityProvisionerTypes), providerService);
            else
                ServiceTypeLog.DomainRegistered(log, nameof(ExternalIdentityProvisionerTypes), declaredOptions.Length, optionNames, providerService);

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
