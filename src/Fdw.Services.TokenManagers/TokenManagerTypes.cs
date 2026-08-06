using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;
using Fdw.Services.TokenManagers.Abstractions;
using Fdw.ServiceTypes.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;

namespace Fdw.Services.TokenManagers;

/// <summary>
/// Collection of token manager service types. Structurally copies <c>SchedulerTypes</c> and is
/// swept by PlatformServices like every other domain — no host registers it by hand. Exactly one
/// token manager is active per deployment (the enabled <c>auth.TokenManager</c> config row); the
/// provider resolves it by name.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(TokenManagerTypeBase<ITokenManager, TokenManagerConfiguration, ITokenManagerFactory<ITokenManager, TokenManagerConfiguration>>),
    typeof(ITokenManagerType),
    typeof(TokenManagerTypes),
    GenerateProvider = true,
    ServiceInterface = typeof(ITokenManager),
    ConfigurationType = typeof(TokenManagerConfiguration),
    ProviderType = typeof(DefaultServiceProvider<ITokenManager, TokenManagerConfiguration, ITokenManagerFactory<ITokenManager, TokenManagerConfiguration>, IServiceConfigurationProvider<TokenManagerConfiguration>>),
    ProviderInterface = typeof(IFdwServiceProvider<ITokenManager, TokenManagerConfiguration>),
    ServiceCategory = "TokenManager")]
public partial class TokenManagerTypes : ServiceTypeCollectionBase<
    TokenManagerTypeBase<ITokenManager, TokenManagerConfiguration, ITokenManagerFactory<ITokenManager, TokenManagerConfiguration>>,
    ITokenManagerType>
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
    static TokenManagerTypes()
    {
        var sweepOptions = RegisterFunc;
        Registration((builder, loggerFactory) =>
        {
            sweepOptions(builder, loggerFactory);
            builder.Services.AddScoped<IFdwServiceProvider<ITokenManager, TokenManagerConfiguration>>(sp =>
            {
                var provider = new DefaultServiceProvider<ITokenManager, TokenManagerConfiguration, ITokenManagerFactory<ITokenManager, TokenManagerConfiguration>, IServiceConfigurationProvider<TokenManagerConfiguration>>(
                    sp,
                    sp.GetService<ILoggerFactory>()?.CreateLogger<DefaultServiceProvider<ITokenManager, TokenManagerConfiguration, ITokenManagerFactory<ITokenManager, TokenManagerConfiguration>, IServiceConfigurationProvider<TokenManagerConfiguration>>>()
                    ?? NullLogger<DefaultServiceProvider<ITokenManager, TokenManagerConfiguration, ITokenManagerFactory<ITokenManager, TokenManagerConfiguration>, IServiceConfigurationProvider<TokenManagerConfiguration>>>.Instance);
                var stLogger = sp.GetService<ILoggerFactory>()?.CreateLogger("TokenManagerTypes");
                try
                {
                    if (sp.GetService<IServiceConfigurationProvider<TokenManagerConfiguration>>() is { } cfgProvider)
                    {
                        // Why the result is read: a provider that did not take its parent still constructs, and
                        // every later read silently misses. The failure has to be said out loud here or nowhere.
                        var parentResult = provider.RegisterParentProvider(cfgProvider);
                        if (!parentResult.IsSuccess && stLogger != null)
                            ServiceTypeLog.FactoryRegistrationFailed(stLogger, "TokenManagerTypes", parentResult.CurrentMessage ?? "TokenManagerTypes");
                    }
                }
                catch (Exception ex)
                {
                    // Why rethrow: a throw here was previously silent, and a provider that failed to take
                    // its parent is unusable in a way that only surfaces much later.
                    if (stLogger != null) ServiceTypeLog.FactoryRegistrationException(stLogger, ex, "TokenManagerTypes");
                    throw;
                }
                return provider;
            });
            return builder;
        });
    }
}
