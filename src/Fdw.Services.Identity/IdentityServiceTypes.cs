using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.ServiceTypes.Logging;
using Fdw.Services.Abstractions;
using Fdw.Services.Identity.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Identity.Commands;

namespace Fdw.Services.Identity;

/// <summary>
/// Collection of managed identity mechanisms — the ways this process can prove its own identity to
/// an external authority in order to call a peer.
/// </summary>
/// <remarks>
/// Unlike <c>TokenManagerTypes</c>, this domain is not a single declared choice: a deployment may
/// hold several identity configurations at once (a different service account per peer, or a
/// federated CI identity alongside a long-lived service identity), each resolved by name through the
/// provider. So it is collected by PlatformServices like every other multi-instance domain.
/// </remarks>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(IdentityServiceTypeBase<IIdentityService, IIdentityServiceImplementationConfiguration, IIdentityServiceFactory<IIdentityService, IIdentityServiceImplementationConfiguration>>),
    typeof(IIdentityServiceType),
    typeof(IdentityServiceTypes),
    ServiceInterface = typeof(IIdentityService),
    ProviderType = typeof(IdentityServiceProvider),
    ProviderInterface = typeof(IIdentityServiceProvider),
    ServiceCategory = "Identity")]
public partial class IdentityServiceTypes : ServiceTypeCollectionBase<
    IdentityServiceTypeBase<IIdentityService, IIdentityServiceImplementationConfiguration, IIdentityServiceFactory<IIdentityService, IIdentityServiceImplementationConfiguration>>,
    IIdentityServiceType>
{
    /// <summary>
    /// The connection this domain's configuration rows are read from and written to.
    /// </summary>
    public static string ConfigurationConnection { get; set; } = "PlatformConfiguration";

    // Configure(), Register(), Initialize() are source-generated.

    /// <summary>
    /// Sets this collection's Register body: the option collect, then this domain's provider, its
    /// configuration provider, and the token cache the options share.
    /// </summary>
    /// <remarks>
    /// The provider is one registration for the whole collection and this declaration already names
    /// it, so the body that registers it is written here beside the declaration. Setting it as the
    /// phase's body is what makes it replaceable: an application calling <c>Registration(...)</c>
    /// replaces the collect and these registrations together.
    /// </remarks>
    static IdentityServiceTypes()
    {
        var collectOptions = RegisterFunc;

        var providerService = typeof(IIdentityServiceProvider).ToString();

        Registration((builder, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<IdentityServiceTypes>() ?? NullLogger<IdentityServiceTypes>.Instance;

            var registered = collectOptions(builder, loggerFactory);
            if (registered.IsFailure)
                return registered;

            builder.Services.TryAddSingleton<IIdentityServiceConfigurationProvider>(sp =>
                new IdentityServiceConfigurationProvider(
                    sp.GetService<ILogger<IdentityServiceConfigurationProvider>>()!,
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    ConfigurationConnection));
            builder.Services.TryAddSingleton<IdentityServiceConfigurationProvider>(
                sp => (IdentityServiceConfigurationProvider)sp.GetRequiredService<IIdentityServiceConfigurationProvider>());
            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<IdentityServiceConfiguration, IdentityServiceConfigurationCommand>>(
                sp => sp.GetRequiredService<IdentityServiceConfigurationProvider>());
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<IdentityServiceConfiguration>>(
                sp => sp.GetRequiredService<IdentityServiceConfigurationProvider>());

            builder.Services.AddSingleton<IIdentityTokenCache>(sp =>
                new IdentityTokenCache(
                    sp.GetService<ILoggerFactory>()?.CreateLogger<IdentityTokenCache>()
                    ?? NullLogger<IdentityTokenCache>.Instance,
                    IdentityTokenCache.DefaultRefreshSkew));

            var declaredOptions = Options;
            var optionNames = string.Join(", ", declaredOptions.Select(option => option.Name));

            ServiceTypeLog.DomainOptionsCollected(log, nameof(IdentityServiceTypes), declaredOptions.Length, optionNames);
            ServiceTypeLog.DomainProviderDeclared(log, nameof(IdentityServiceTypes), providerService);

            builder.Services.AddScoped<IIdentityServiceProvider>(sp =>
            {
                var provider = new IdentityServiceProvider(
                    sp,
                    sp.GetService<ILoggerFactory>()?.CreateLogger<IdentityServiceProvider>()
                    ?? NullLogger<IdentityServiceProvider>.Instance);

                var stLogger = sp.GetService<ILoggerFactory>()?.CreateLogger<IdentityServiceTypes>()
                    ?? NullLogger<IdentityServiceTypes>.Instance;
                ServiceTypeLog.DomainProviderConstructing(stLogger, nameof(IdentityServiceTypes), provider.GetType().Name);
                try
                {
                    if (sp.GetService<IIdentityServiceConfigurationProvider>() is { } cfgProvider)
                    {
                        var domainResult = provider.Register(cfgProvider);
                        if (domainResult.IsSuccess)
                            ServiceTypeLog.DomainConfigurationSourceAttached(stLogger, nameof(IdentityServiceTypes), provider.GetType().Name, cfgProvider.GetType().Name);
                        else
                            ServiceTypeLog.DomainConfigurationSourceRejected(stLogger, nameof(IdentityServiceTypes), provider.GetType().Name, cfgProvider.GetType().Name, domainResult.CurrentMessage);
                    }
                    else
                    {
                        ServiceTypeLog.DomainHasNoConfigurationSource(
                            stLogger,
                            nameof(IdentityServiceTypes),
                            provider.GetType().Name,
                            typeof(IServiceConfigurationProvider<IdentityServiceConfiguration>).ToString());
                    }
                }
                catch (Exception ex)
                {
                    ServiceTypeLog.FactoryRegistrationException(stLogger, ex, nameof(IdentityServiceTypes));
                    throw;
                }
                return provider;
            });

            if (declaredOptions.Length == 0)
                ServiceTypeLog.DomainRegisteredWithNoOptions(log, nameof(IdentityServiceTypes), providerService);
            else
                ServiceTypeLog.DomainRegistered(log, nameof(IdentityServiceTypes), declaredOptions.Length, optionNames, providerService);

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
