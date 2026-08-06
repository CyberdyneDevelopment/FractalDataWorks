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

namespace Fdw.Services.ExternalIdentityProviders;

/// <summary>
/// Collection of external identity provider service types. Structurally copies
/// <c>TokenManagerTypes</c> and is swept by PlatformServices like every other domain — no host
/// registers it by hand. Unlike TokenManagers, this is NOT a "declared choice" domain: multiple
/// <c>auth.ExternalIdentityProvider</c> config rows may be simultaneously active, and the caller
/// (<c>ConnectTokenEndpointBase</c>, via <see cref="ExternalIdentityProviderResolver"/>) selects one by
/// name or, when exactly one is active, uses that one implicitly.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(ExternalIdentityProviderTypeBase<IExternalIdentityProvider, ExternalIdentityProviderConfiguration, IExternalIdentityProviderFactory<IExternalIdentityProvider, ExternalIdentityProviderConfiguration>>),
    typeof(IExternalIdentityProviderType),
    typeof(ExternalIdentityProviderTypes),
    GenerateProvider = true,
    ServiceInterface = typeof(IExternalIdentityProvider),
    ConfigurationType = typeof(ExternalIdentityProviderConfiguration),
    ProviderType = typeof(DefaultServiceProvider<IExternalIdentityProvider, ExternalIdentityProviderConfiguration, IExternalIdentityProviderFactory<IExternalIdentityProvider, ExternalIdentityProviderConfiguration>, IServiceConfigurationProvider<ExternalIdentityProviderConfiguration>>),
    ProviderInterface = typeof(IFdwServiceProvider<IExternalIdentityProvider, ExternalIdentityProviderConfiguration>),
    ServiceCategory = "ExternalIdentityProvider")]
public partial class ExternalIdentityProviderTypes : ServiceTypeCollectionBase<
    ExternalIdentityProviderTypeBase<IExternalIdentityProvider, ExternalIdentityProviderConfiguration, IExternalIdentityProviderFactory<IExternalIdentityProvider, ExternalIdentityProviderConfiguration>>,
    IExternalIdentityProviderType>
{
    // Configure(), Register(), Initialize() are source-generated.

    /// <summary>
    /// Sets this collection's Register body: the option sweep, then this domain's provider.
    /// </summary>
    /// <remarks>
    /// The provider is one registration for the whole collection and this declaration already names it,
    /// so the body that registers it is written here beside the declaration. Setting it as the phase's
    /// body is what makes it replaceable: an application calling <c>Registration(...)</c> replaces the
    /// sweep and this registration together, which is the correct semantic for a host taking over phase 2.
    /// </remarks>
    static ExternalIdentityProviderTypes()
    {
        var sweepOptions = RegisterFunc;
        Registration((builder, loggerFactory) =>
        {
            sweepOptions(builder, loggerFactory);
            builder.Services.AddScoped<IFdwServiceProvider<IExternalIdentityProvider, ExternalIdentityProviderConfiguration>>(sp =>
            {
                var provider = new DefaultServiceProvider<IExternalIdentityProvider, ExternalIdentityProviderConfiguration, IExternalIdentityProviderFactory<IExternalIdentityProvider, ExternalIdentityProviderConfiguration>, IServiceConfigurationProvider<ExternalIdentityProviderConfiguration>>(
                    sp,
                    sp.GetService<ILoggerFactory>()?.CreateLogger<DefaultServiceProvider<IExternalIdentityProvider, ExternalIdentityProviderConfiguration, IExternalIdentityProviderFactory<IExternalIdentityProvider, ExternalIdentityProviderConfiguration>, IServiceConfigurationProvider<ExternalIdentityProviderConfiguration>>>()
                    ?? NullLogger<DefaultServiceProvider<IExternalIdentityProvider, ExternalIdentityProviderConfiguration, IExternalIdentityProviderFactory<IExternalIdentityProvider, ExternalIdentityProviderConfiguration>, IServiceConfigurationProvider<ExternalIdentityProviderConfiguration>>>.Instance);
                var stLogger = sp.GetService<ILoggerFactory>()?.CreateLogger("ExternalIdentityProviderTypes");
                try
                {
                    if (sp.GetService<IServiceConfigurationProvider<ExternalIdentityProviderConfiguration>>() is { } cfgProvider)
                    {
                        // Why the result is read: a provider that did not take its parent still constructs, and
                        // every later read silently misses. The failure has to be said out loud here or nowhere.
                        var parentResult = provider.RegisterParentProvider(cfgProvider);
                        if (!parentResult.IsSuccess && stLogger != null)
                            ServiceTypeLog.FactoryRegistrationFailed(stLogger, "ExternalIdentityProviderTypes", parentResult.CurrentMessage ?? "ExternalIdentityProviderTypes");
                    }
                }
                catch (Exception ex)
                {
                    // Why rethrow: a throw here was previously silent, and a provider that failed to take
                    // its parent is unusable in a way that only surfaces much later.
                    if (stLogger != null) ServiceTypeLog.FactoryRegistrationException(stLogger, ex, "ExternalIdentityProviderTypes");
                    throw;
                }
                return provider;
            });
            return builder;
        });
    }
}
