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
using System.Linq;
using Fdw.Results;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.TokenManagers.Commands;

namespace Fdw.Services.TokenManagers;

/// <summary>
/// Collection of token manager service types. Structurally copies <c>SchedulerTypes</c> and is
/// collected by PlatformServices like every other domain — no host registers it by hand. Exactly one
/// token manager is active per deployment (the enabled <c>auth.TokenManager</c> config row); the
/// provider resolves it by name.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(TokenManagerTypeBase<ITokenManager, ITokenManagerImplementationConfiguration, ITokenManagerFactory<ITokenManager, ITokenManagerImplementationConfiguration>>),
    typeof(ITokenManagerType),
    typeof(TokenManagerTypes),
    ServiceInterface = typeof(ITokenManager),
    ProviderType = typeof(TokenManagerProvider),
    ProviderInterface = typeof(ITokenManagerProvider),
    ServiceCategory = "TokenManager")]
public partial class TokenManagerTypes : ServiceTypeCollectionBase<
    TokenManagerTypeBase<ITokenManager, ITokenManagerImplementationConfiguration, ITokenManagerFactory<ITokenManager, ITokenManagerImplementationConfiguration>>,
    ITokenManagerType>
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
    static TokenManagerTypes()
    {
        var collectOptions = RegisterFunc;

        var providerService = typeof(ITokenManagerProvider).ToString();

        Registration((builder, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<TokenManagerTypes>() ?? NullLogger<TokenManagerTypes>.Instance;

            var registered = collectOptions(builder, loggerFactory);
            if (registered.IsFailure)
                return registered;

            var declaredOptions = Options;
            var optionNames = string.Join(", ", declaredOptions.Select(option => option.Name));

            ServiceTypeLog.DomainOptionsCollected(log, nameof(TokenManagerTypes), declaredOptions.Length, optionNames);
            ServiceTypeLog.DomainProviderDeclared(log, nameof(TokenManagerTypes), providerService);

            builder.Services.TryAddSingleton<ITokenManagerConfigurationProvider>(sp =>
                new TokenManagerConfigurationProvider(
                    sp.GetService<ILogger<TokenManagerConfigurationProvider>>()!,
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    ConfigurationConnection));
            builder.Services.TryAddSingleton<TokenManagerConfigurationProvider>(
                sp => (TokenManagerConfigurationProvider)sp.GetRequiredService<ITokenManagerConfigurationProvider>());
            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<TokenManagerConfiguration, TokenManagerConfigurationCommand>>(
                sp => sp.GetRequiredService<TokenManagerConfigurationProvider>());
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<TokenManagerConfiguration>>(
                sp => sp.GetRequiredService<TokenManagerConfigurationProvider>());

            builder.Services.AddScoped<ITokenManagerProvider>(sp =>
            {
                var provider = new TokenManagerProvider(
                    sp,
                    sp.GetService<ILoggerFactory>()?.CreateLogger<TokenManagerProvider>()
                    ?? NullLogger<TokenManagerProvider>.Instance);

                var stLogger = sp.GetService<ILoggerFactory>()?.CreateLogger<TokenManagerTypes>()
                    ?? NullLogger<TokenManagerTypes>.Instance;
                ServiceTypeLog.DomainProviderConstructing(stLogger, nameof(TokenManagerTypes), provider.GetType().Name);
                try
                {
                    if (sp.GetService<ITokenManagerConfigurationProvider>() is { } cfgProvider)
                    {
                        var domainResult = provider.Register(cfgProvider);
                        if (domainResult.IsSuccess)
                            ServiceTypeLog.DomainConfigurationSourceAttached(stLogger, nameof(TokenManagerTypes), provider.GetType().Name, cfgProvider.GetType().Name);
                        else
                            ServiceTypeLog.DomainConfigurationSourceRejected(stLogger, nameof(TokenManagerTypes), provider.GetType().Name, cfgProvider.GetType().Name, domainResult.CurrentMessage);
                    }
                    else
                    {
                        ServiceTypeLog.DomainHasNoConfigurationSource(
                            stLogger,
                            nameof(TokenManagerTypes),
                            provider.GetType().Name,
                            typeof(IServiceConfigurationProvider<TokenManagerConfiguration>).ToString());
                    }
                }
                catch (Exception ex)
                {
                    ServiceTypeLog.FactoryRegistrationException(stLogger, ex, nameof(TokenManagerTypes));
                    throw;
                }
                return provider;
            });

            if (declaredOptions.Length == 0)
                ServiceTypeLog.DomainRegisteredWithNoOptions(log, nameof(TokenManagerTypes), providerService);
            else
                ServiceTypeLog.DomainRegistered(log, nameof(TokenManagerTypes), declaredOptions.Length, optionNames, providerService);

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
