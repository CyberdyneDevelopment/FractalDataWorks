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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Commands;

namespace Fdw.Services.ExternalIdentityProviders;

/// <summary>
/// Collection of external identity provider service types. Structurally copies
/// <c>TokenManagerTypes</c> and is collected by PlatformServices like every other domain — no host
/// registers it by hand. Unlike TokenManagers, this is NOT a "declared choice" domain: multiple
/// <c>auth.ExternalIdentityProvider</c> config rows may be simultaneously active, and the caller
/// (<c>ConnectTokenEndpointBase</c>, via <see cref="ExternalIdentityProviderResolver"/>) selects one by
/// name or, when exactly one is active, uses that one implicitly.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(ExternalIdentityProviderTypeBase<IExternalIdentityProvider, IExternalIdentityProviderImplementationConfiguration, IExternalIdentityProviderFactory<IExternalIdentityProvider, IExternalIdentityProviderImplementationConfiguration>>),
    typeof(IExternalIdentityProviderType),
    typeof(ExternalIdentityProviderTypes),
    ServiceInterface = typeof(IExternalIdentityProvider),
    ProviderType = typeof(ExternalIdentityProviderServiceProvider),
    ProviderInterface = typeof(IExternalIdentityProviderServiceProvider),
    ServiceCategory = "ExternalIdentityProvider")]
public partial class ExternalIdentityProviderTypes : ServiceTypeCollectionBase<
    ExternalIdentityProviderTypeBase<IExternalIdentityProvider, IExternalIdentityProviderImplementationConfiguration, IExternalIdentityProviderFactory<IExternalIdentityProvider, IExternalIdentityProviderImplementationConfiguration>>,
    IExternalIdentityProviderType>
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
    static ExternalIdentityProviderTypes()
    {
        var collectOptions = RegisterFunc;

        var providerService = typeof(IPlatformServiceProvider<IExternalIdentityProvider, IExternalIdentityProviderImplementationConfiguration>).ToString();

        Registration((builder, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<ExternalIdentityProviderTypes>() ?? NullLogger<ExternalIdentityProviderTypes>.Instance;

            var registered = collectOptions(builder, loggerFactory);
            if (registered.IsFailure)
                return registered;

            var declaredOptions = Options;
            var optionNames = string.Join(", ", declaredOptions.Select(option => option.Name));

            ServiceTypeLog.DomainOptionsCollected(log, nameof(ExternalIdentityProviderTypes), declaredOptions.Length, optionNames);
            ServiceTypeLog.DomainProviderDeclared(log, nameof(ExternalIdentityProviderTypes), providerService);

            builder.Services.TryAddSingleton<IExternalIdentityProviderConfigurationProvider>(sp =>
                new ExternalIdentityProviderConfigurationProvider(
                    sp.GetService<ILogger<ExternalIdentityProviderConfigurationProvider>>()!,
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    ConfigurationConnection));
            builder.Services.TryAddSingleton<ExternalIdentityProviderConfigurationProvider>(
                sp => (ExternalIdentityProviderConfigurationProvider)sp.GetRequiredService<IExternalIdentityProviderConfigurationProvider>());
            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<ExternalIdentityProviderConfiguration, ExternalIdentityProviderConfigurationCommand>>(
                sp => sp.GetRequiredService<ExternalIdentityProviderConfigurationProvider>());
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<ExternalIdentityProviderConfiguration>>(
                sp => sp.GetRequiredService<ExternalIdentityProviderConfigurationProvider>());

            ExternalIdentityProviderResolver.RegisterDomainServices(builder.Services);

            builder.Services.AddScoped<IPlatformServiceProvider<IExternalIdentityProvider, IExternalIdentityProviderImplementationConfiguration>>(sp =>
            {
                var provider = new ExternalIdentityProviderServiceProvider(
                    sp,
                    sp.GetService<ILoggerFactory>()?.CreateLogger<ExternalIdentityProviderServiceProvider>()
                    ?? NullLogger<ExternalIdentityProviderServiceProvider>.Instance);

                var stLogger = sp.GetService<ILoggerFactory>()?.CreateLogger<ExternalIdentityProviderTypes>()
                    ?? NullLogger<ExternalIdentityProviderTypes>.Instance;
                ServiceTypeLog.DomainProviderConstructing(stLogger, nameof(ExternalIdentityProviderTypes), provider.GetType().Name);
                try
                {
                    if (sp.GetService<IExternalIdentityProviderConfigurationProvider>() is { } cfgProvider)
                    {
                        var domainResult = provider.Register(cfgProvider);
                        if (domainResult.IsSuccess)
                            ServiceTypeLog.DomainConfigurationSourceAttached(stLogger, nameof(ExternalIdentityProviderTypes), provider.GetType().Name, cfgProvider.GetType().Name);
                        else
                            ServiceTypeLog.DomainConfigurationSourceRejected(stLogger, nameof(ExternalIdentityProviderTypes), provider.GetType().Name, cfgProvider.GetType().Name, domainResult.CurrentMessage);
                    }
                    else
                    {
                        ServiceTypeLog.DomainHasNoConfigurationSource(
                            stLogger,
                            nameof(ExternalIdentityProviderTypes),
                            provider.GetType().Name,
                            typeof(IServiceConfigurationProvider<ExternalIdentityProviderConfiguration>).ToString());
                    }
                }
                catch (Exception ex)
                {
                    ServiceTypeLog.FactoryRegistrationException(stLogger, ex, nameof(ExternalIdentityProviderTypes));
                    throw;
                }
                return provider;
            });

            if (declaredOptions.Length == 0)
                ServiceTypeLog.DomainRegisteredWithNoOptions(log, nameof(ExternalIdentityProviderTypes), providerService);
            else
                ServiceTypeLog.DomainRegistered(log, nameof(ExternalIdentityProviderTypes), declaredOptions.Length, optionNames, providerService);

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
