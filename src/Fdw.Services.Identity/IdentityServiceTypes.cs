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
    typeof(IdentityServiceTypeBase<IIdentityService, IdentityServiceConfiguration, IIdentityServiceFactory<IIdentityService, IdentityServiceConfiguration>>),
    typeof(IIdentityServiceType),
    typeof(IdentityServiceTypes),
    GenerateProvider = true,
    ServiceInterface = typeof(IIdentityService),
    ConfigurationType = typeof(IdentityServiceConfiguration),
    ProviderType = typeof(DefaultServiceProvider<IIdentityService, IdentityServiceConfiguration, IIdentityServiceFactory<IIdentityService, IdentityServiceConfiguration>, IServiceConfigurationProvider<IdentityServiceConfiguration>>),
    ProviderInterface = typeof(IFdwServiceProvider<IIdentityService, IdentityServiceConfiguration>),
    ServiceCategory = "Identity")]
public partial class IdentityServiceTypes : ServiceTypeCollectionBase<
    IdentityServiceTypeBase<IIdentityService, IdentityServiceConfiguration, IIdentityServiceFactory<IIdentityService, IdentityServiceConfiguration>>,
    IIdentityServiceType>
{
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

        // Why a local: this closed generic is the DI key a consumer injects, and it is reported at
        // three points below. Written out three times it is three chances for them to disagree.
        var providerService = typeof(IFdwServiceProvider<IIdentityService, IdentityServiceConfiguration>).ToString();

        Registration((builder, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<IdentityServiceTypes>() ?? NullLogger<IdentityServiceTypes>.Instance;

            // Why the result is read: discarding it meant an option that failed to register was
            // followed by this body registering the provider anyway and reporting success.
            var registered = collectOptions(builder, loggerFactory);
            if (registered.IsFailure)
                return registered;

            IdentityServiceConfigurationProvider.RegisterDomainServices(builder.Services);

            // Why singleton: the cache's whole purpose is that one live token is reused across every
            // outbound call in the process. A scoped cache would acquire a new token per scope, which
            // is the behaviour it exists to prevent.
            builder.Services.AddSingleton<IIdentityTokenCache>(sp =>
                new IdentityTokenCache(
                    sp.GetService<ILoggerFactory>()?.CreateLogger<IdentityTokenCache>()
                    ?? NullLogger<IdentityTokenCache>.Instance,
                    IdentityTokenCache.DefaultRefreshSkew));

            var declaredOptions = Options;
            var optionNames = string.Join(", ", declaredOptions.Select(option => option.Name));

            ServiceTypeLog.DomainOptionsCollected(log, nameof(IdentityServiceTypes), declaredOptions.Length, optionNames);
            ServiceTypeLog.DomainProviderDeclared(log, nameof(IdentityServiceTypes), providerService);

            builder.Services.AddScoped<IFdwServiceProvider<IIdentityService, IdentityServiceConfiguration>>(sp =>
            {
                var provider = new DefaultServiceProvider<IIdentityService, IdentityServiceConfiguration, IIdentityServiceFactory<IIdentityService, IdentityServiceConfiguration>, IServiceConfigurationProvider<IdentityServiceConfiguration>>(
                    sp,
                    sp.GetService<ILoggerFactory>()?.CreateLogger<DefaultServiceProvider<IIdentityService, IdentityServiceConfiguration, IIdentityServiceFactory<IIdentityService, IdentityServiceConfiguration>, IServiceConfigurationProvider<IdentityServiceConfiguration>>>()
                    ?? NullLogger<DefaultServiceProvider<IIdentityService, IdentityServiceConfiguration, IIdentityServiceFactory<IIdentityService, IdentityServiceConfiguration>, IServiceConfigurationProvider<IdentityServiceConfiguration>>>.Instance);

                var stLogger = sp.GetService<ILoggerFactory>()?.CreateLogger<IdentityServiceTypes>()
                    ?? NullLogger<IdentityServiceTypes>.Instance;
                ServiceTypeLog.DomainProviderConstructing(stLogger, nameof(IdentityServiceTypes), provider.GetType().Name);
                try
                {
                    if (sp.GetService<IServiceConfigurationProvider<IdentityServiceConfiguration>>() is { } cfgProvider)
                    {
                        // Why the result is read: a provider that did not take its parent still
                        // constructs, and every later read silently misses.
                        var parentResult = provider.Register(cfgProvider);
                        if (parentResult.IsSuccess)
                            ServiceTypeLog.DomainConfigurationSourceAttached(stLogger, nameof(IdentityServiceTypes), provider.GetType().Name, cfgProvider.GetType().Name);
                        else
                            ServiceTypeLog.DomainConfigurationSourceRejected(stLogger, nameof(IdentityServiceTypes), provider.GetType().Name, cfgProvider.GetType().Name, parentResult.CurrentMessage);
                    }
                    else
                    {
                        // Why Critical, and why the collection says it rather than the provider: from
                        // inside the provider a null parent is indistinguishable from a domain that
                        // needs none. Without it the domain fails every lookup by name for the life
                        // of the scope with nothing pointing back here.
                        ServiceTypeLog.DomainHasNoConfigurationSource(
                            stLogger,
                            nameof(IdentityServiceTypes),
                            provider.GetType().Name,
                            typeof(IServiceConfigurationProvider<IdentityServiceConfiguration>).ToString());
                    }
                }
                catch (Exception ex)
                {
                    // Why rethrow: a provider that failed to take its parent is unusable in a way
                    // that only surfaces much later.
                    ServiceTypeLog.FactoryRegistrationException(stLogger, ex, nameof(IdentityServiceTypes));
                    throw;
                }
                return provider;
            });

            // Why the milestone comes after the registration: it states that the domain finished
            // phase 2, which is only true once the provider is actually in the container.
            if (declaredOptions.Length == 0)
                ServiceTypeLog.DomainRegisteredWithNoOptions(log, nameof(IdentityServiceTypes), providerService);
            else
                ServiceTypeLog.DomainRegistered(log, nameof(IdentityServiceTypes), declaredOptions.Length, optionNames, providerService);

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
