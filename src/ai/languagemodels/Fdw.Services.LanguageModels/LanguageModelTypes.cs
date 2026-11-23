using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.ServiceTypes.Logging;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.LanguageModels.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.LanguageModels;

/// <summary>
/// Collection of language model provider types. A provider package declares an option with <c>[Implementation(typeof(LanguageModelTypes), "...")]</c>;
/// referencing the package is what registers it.
/// </summary>
/// <remarks>
/// <c>RestrictToCurrentCompilation</c> is <c>true</c>: no provider lives in this assembly, so the collection compiles with an empty option set.
/// Provider options arrive cross-assembly through the entry-point app's Registration.SourceGenerators module initializer.
/// </remarks>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(LanguageModelTypeBase<ILanguageModel, ILanguageModelFactory<ILanguageModel, ILanguageModelImplementationConfiguration>, ILanguageModelImplementationConfiguration>),
    typeof(ILanguageModelType),
    typeof(LanguageModelTypes),
    ServiceInterface = typeof(ILanguageModel),
    ProviderType = typeof(LanguageModelProvider),
    ProviderInterface = typeof(ILanguageModelProvider),
    ServiceCategory = "LanguageModel",
    RestrictToCurrentCompilation = true)]
public partial class LanguageModelTypes : ServiceTypeCollectionBase<
    LanguageModelTypeBase<ILanguageModel, ILanguageModelFactory<ILanguageModel, ILanguageModelImplementationConfiguration>, ILanguageModelImplementationConfiguration>,
    ILanguageModelType<ILanguageModel, ILanguageModelFactory<ILanguageModel, ILanguageModelImplementationConfiguration>, ILanguageModelImplementationConfiguration>>
{
    /// <summary>The connection this domain's configuration rows are read from and written to.</summary>
    public static string ConfigurationConnection => PlatformConnection.Name;

    // Configure(), Register() and Initialize() are source-generated

    /// <summary>Sets this collection's Register body: the option collect, then this domain's provider.</summary>
    /// <remarks>
    /// Setting it as the phase's body is what makes it replaceable: an application calling <c>Registration(...)</c> replaces the collect and this
    /// registration together.
    /// </remarks>
    static LanguageModelTypes()
    {
        var providerService = typeof(ILanguageModelProvider).ToString();

        Registration((builder, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<LanguageModelTypes>() ?? NullLogger<LanguageModelTypes>.Instance;

            builder.Services.TryAddSingleton<ILanguageModelConfigurationProvider>(sp =>
                new LanguageModelConfigurationProvider(
                    sp.GetRequiredService<ILogger<LanguageModelConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(), LanguageModelTypes.ConfigurationConnection, "agent", "LanguageModel"));

            // The domain's own base registration goes FIRST -- each option decorates this registration (wraps its factory) from its own
            // Register() call below, so it has to already exist in builder.Services by the time that runs.
            builder.Services.AddScoped<ILanguageModelProvider>(sp =>
            {
                var provider = new LanguageModelProvider(
                    sp.GetService<ILoggerFactory>()?.CreateLogger<LanguageModelProvider>()
                    ?? NullLogger<LanguageModelProvider>.Instance);

                var stLogger = sp.GetService<ILoggerFactory>()?.CreateLogger<LanguageModelTypes>()
                    ?? NullLogger<LanguageModelTypes>.Instance;
                ServiceTypeLog.DomainProviderConstructing(stLogger, nameof(LanguageModelTypes), provider.GetType().Name);
                try
                {
                    if (sp.GetService<ILanguageModelConfigurationProvider>() is { } cfgProvider)
                    {
                        var domainResult = provider.Register(cfgProvider);
                        if (domainResult.IsSuccess)
                            ServiceTypeLog.DomainConfigurationSourceAttached(stLogger, nameof(LanguageModelTypes), provider.GetType().Name, cfgProvider.GetType().Name);
                        else
                            ServiceTypeLog.DomainConfigurationSourceRejected(stLogger, nameof(LanguageModelTypes), provider.GetType().Name, cfgProvider.GetType().Name, domainResult.CurrentMessage);
                    }
                    else
                    {
                        ServiceTypeLog.DomainHasNoConfigurationSource(
                            stLogger,
                            nameof(LanguageModelTypes),
                            provider.GetType().Name,
                            typeof(IImplementationConfigurationProvider<ILanguageModelImplementationConfiguration>).ToString());
                    }
                }
                catch (Exception ex)
                {
                    ServiceTypeLog.FactoryRegistrationException(stLogger, ex, nameof(LanguageModelTypes));
                    throw;
                }

                return provider;
            });

            foreach (var option in Options)
            {
                var optionRegistered = option.Register(builder, loggerFactory);
                if (optionRegistered.IsFailure)
                    return optionRegistered;
            }

            var declaredOptions = Options;
            var optionNames = string.Join(", ", declaredOptions.Select(option => option.Name));

            ServiceTypeLog.DomainOptionsCollected(log, nameof(LanguageModelTypes), declaredOptions.Length, optionNames);
            ServiceTypeLog.DomainProviderDeclared(log, nameof(LanguageModelTypes), providerService);

            if (declaredOptions.Length == 0)
                ServiceTypeLog.DomainRegisteredWithNoOptions(log, nameof(LanguageModelTypes), providerService);
            else
                ServiceTypeLog.DomainRegistered(log, nameof(LanguageModelTypes), declaredOptions.Length, optionNames, providerService);

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
