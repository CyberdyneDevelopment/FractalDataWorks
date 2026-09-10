using System.Linq;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Logging.Abstractions;
using Fdw.ServiceTypes;
using Fdw.ServiceTypes.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Logging;

/// <summary>
/// The logging option set — one option per logging implementation.
/// </summary>
/// <remarks>
/// Logging is the earliest domain to come up, so its configuration is read through the
/// <c>ServerConfiguration</c> connection declared in <c>configurationSchema.json</c> rather than
/// <c>PlatformConfiguration</c>: a logging pipeline has to exist before the platform store is
/// reachable.
/// </remarks>
[ServiceTypeCollection(
    typeof(LoggingTypeBase<ILoggingService, ILoggingImplementationConfiguration, ILoggingFactory<ILoggingService, ILoggingImplementationConfiguration>>),
    typeof(ILoggingType),
    typeof(LoggingTypes),
    ServiceInterface = typeof(ILoggingService),
    ProviderType = typeof(LoggingServiceProvider),
    ProviderInterface = typeof(ILoggingServiceProvider),
    ServiceCategory = "Logging")]
public partial class LoggingTypes : ServiceTypeCollectionBase<
    LoggingTypeBase<ILoggingService, ILoggingImplementationConfiguration, ILoggingFactory<ILoggingService, ILoggingImplementationConfiguration>>,
    ILoggingType>
{
    /// <summary>The connection this domain's configuration rows are read from and written to.</summary>
    public static string ConfigurationConnection { get; set; } = "ServerConfiguration";

    static LoggingTypes()
    {
        var collectOptions = RegisterFunc;

        var providerService = typeof(ILoggingServiceProvider).ToString();

        Registration((builder, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<LoggingTypes>() ?? NullLogger<LoggingTypes>.Instance;

            var registered = collectOptions(builder, loggerFactory);
            if (registered.IsFailure)
                return registered;

            builder.Services.TryAddSingleton<ILoggingConfigurationProvider>(sp =>
                new LoggingConfigurationProvider(
                    sp.GetService<ILogger<LoggingConfigurationProvider>>()!,
                    sp.GetRequiredService<IConfigurationGatewayProvider>()));
            builder.Services.TryAddSingleton<LoggingConfigurationProvider>(
                sp => (LoggingConfigurationProvider)sp.GetRequiredService<ILoggingConfigurationProvider>());

            var declaredOptions = Options;
            var optionNames = string.Join(", ", declaredOptions.Select(option => option.Name));

            ServiceTypeLog.DomainOptionsCollected(log, nameof(LoggingTypes), declaredOptions.Length, optionNames);
            ServiceTypeLog.DomainProviderDeclared(log, nameof(LoggingTypes), providerService);

            builder.Services.AddScoped<ILoggingServiceProvider>(sp =>
            {
                var provider = new LoggingServiceProvider(
                    sp,
                    sp.GetService<ILogger<LoggingServiceProvider>>()
                    ?? NullLogger<LoggingServiceProvider>.Instance);

                var stLogger = sp.GetService<ILoggerFactory>()?.CreateLogger<LoggingTypes>()
                    ?? NullLogger<LoggingTypes>.Instance;
                ServiceTypeLog.DomainProviderConstructing(stLogger, nameof(LoggingTypes), provider.GetType().Name);
                if (sp.GetService<ILoggingConfigurationProvider>() is { } cfgProvider)
                {
                    var domainResult = provider.Register(cfgProvider);
                    if (domainResult.IsSuccess)
                        ServiceTypeLog.DomainConfigurationSourceAttached(stLogger, nameof(LoggingTypes), provider.GetType().Name, cfgProvider.GetType().Name);
                    else
                        ServiceTypeLog.DomainConfigurationSourceRejected(stLogger, nameof(LoggingTypes), provider.GetType().Name, cfgProvider.GetType().Name, domainResult.CurrentMessage);
                }
                else
                {
                    ServiceTypeLog.DomainHasNoConfigurationSource(
                        stLogger,
                        nameof(LoggingTypes),
                        provider.GetType().Name,
                        typeof(IImplementationConfigurationProvider<ILoggingImplementationConfiguration>).ToString());
                }

                return provider;
            });

            if (declaredOptions.Length == 0)
                ServiceTypeLog.DomainRegisteredWithNoOptions(log, nameof(LoggingTypes), providerService);
            else
                ServiceTypeLog.DomainRegistered(log, nameof(LoggingTypes), declaredOptions.Length, optionNames, providerService);

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
