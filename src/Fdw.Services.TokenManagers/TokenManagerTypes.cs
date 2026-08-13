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

namespace Fdw.Services.TokenManagers;

/// <summary>
/// Collection of token manager service types. Structurally copies <c>SchedulerTypes</c> and is
/// collected by PlatformServices like every other domain — no host registers it by hand. Exactly one
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

        // Why a local: this closed generic is the DI key a consumer injects, and it is reported at
        // three points below — the deferred declaration, the milestone, and the zero-option warning.
        // Written out three times it is three chances for them to disagree.
        var providerService = typeof(IFdwServiceProvider<ITokenManager, TokenManagerConfiguration>).ToString();

        Registration((builder, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<TokenManagerTypes>() ?? NullLogger<TokenManagerTypes>.Instance;

            // Why the result is read: this replacement calls the func it captured, and discarding
            // what that returned meant an option that failed to register was followed by this body
            // registering the provider anyway and reporting success.
            var registered = collectOptions(builder, loggerFactory);
            if (registered.IsFailure)
                return registered;

            var declaredOptions = Options;
            var optionNames = string.Join(", ", declaredOptions.Select(option => option.Name));

            ServiceTypeLog.DomainOptionsCollected(log, nameof(TokenManagerTypes), declaredOptions.Length, optionNames);
            ServiceTypeLog.DomainProviderDeclared(log, nameof(TokenManagerTypes), providerService);

            builder.Services.AddScoped<IFdwServiceProvider<ITokenManager, TokenManagerConfiguration>>(sp =>
            {
                var provider = new DefaultServiceProvider<ITokenManager, TokenManagerConfiguration, ITokenManagerFactory<ITokenManager, TokenManagerConfiguration>, IServiceConfigurationProvider<TokenManagerConfiguration>>(
                    sp,
                    sp.GetService<ILoggerFactory>()?.CreateLogger<DefaultServiceProvider<ITokenManager, TokenManagerConfiguration, ITokenManagerFactory<ITokenManager, TokenManagerConfiguration>, IServiceConfigurationProvider<TokenManagerConfiguration>>>()
                    ?? NullLogger<DefaultServiceProvider<ITokenManager, TokenManagerConfiguration, ITokenManagerFactory<ITokenManager, TokenManagerConfiguration>, IServiceConfigurationProvider<TokenManagerConfiguration>>>.Instance);

                // Why ILogger<TokenManagerTypes> and not CreateLogger("TokenManagerTypes"): SourceContext then
                // carries the namespace-qualified collection, and the category cannot drift from the
                // type it claims to name. The provider logs its own lines under its own type, so the
                // two layers read base-then-derived rather than collapsing onto one category.
                var stLogger = sp.GetService<ILoggerFactory>()?.CreateLogger<TokenManagerTypes>()
                    ?? NullLogger<TokenManagerTypes>.Instance;
                ServiceTypeLog.DomainProviderConstructing(stLogger, nameof(TokenManagerTypes), provider.GetType().Name);
                try
                {
                    if (sp.GetService<IServiceConfigurationProvider<TokenManagerConfiguration>>() is { } cfgProvider)
                    {
                        // Why the result is read: a provider that did not take its parent still constructs, and
                        // every later read silently misses. The failure has to be said out loud here or nowhere.
                        var parentResult = provider.Register(cfgProvider);
                        if (parentResult.IsSuccess)
                            ServiceTypeLog.DomainConfigurationSourceAttached(stLogger, nameof(TokenManagerTypes), provider.GetType().Name, cfgProvider.GetType().Name);
                        else
                            ServiceTypeLog.DomainConfigurationSourceRejected(stLogger, nameof(TokenManagerTypes), provider.GetType().Name, cfgProvider.GetType().Name, parentResult.CurrentMessage);
                    }
                    else
                    {
                        // Why Critical, and why the collection says it rather than the provider: from
                        // inside the provider a null parent is indistinguishable from a domain that needs
                        // none. This is the one place that knows one was meant to arrive, and without it
                        // the domain fails every lookup by name for the life of the scope with nothing
                        // pointing back here.
                        ServiceTypeLog.DomainHasNoConfigurationSource(
                            stLogger,
                            nameof(TokenManagerTypes),
                            provider.GetType().Name,
                            typeof(IServiceConfigurationProvider<TokenManagerConfiguration>).ToString());
                    }
                }
                catch (Exception ex)
                {
                    // Why rethrow: a throw here was previously silent, and a provider that failed to take
                    // its parent is unusable in a way that only surfaces much later.
                    ServiceTypeLog.FactoryRegistrationException(stLogger, ex, nameof(TokenManagerTypes));
                    throw;
                }
                return provider;
            });

            // Why the milestone comes after the registration and not before: it states that the domain
            // finished phase 2, which is only true once the provider is actually in the container.
            if (declaredOptions.Length == 0)
                ServiceTypeLog.DomainRegisteredWithNoOptions(log, nameof(TokenManagerTypes), providerService);
            else
                ServiceTypeLog.DomainRegistered(log, nameof(TokenManagerTypes), declaredOptions.Length, optionNames, providerService);

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
