using System.Linq;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Telemetry.Abstractions;
using Fdw.ServiceTypes;
using Fdw.ServiceTypes.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Telemetry;

/// <summary>
/// The telemetry option set — one option per telemetry implementation.
/// </summary>
/// <remarks>
/// Telemetry is the earliest domain to come up, so its configuration is read through the
/// <c>ServerConfiguration</c> connection declared in <c>configurationSchema.json</c> rather than
/// <c>PlatformConfiguration</c>: a telemetry pipeline has to exist before the platform store is
/// reachable.
/// </remarks>
[ServiceTypeCollection(
    typeof(TelemetryTypeBase<ITelemetryService, ITelemetryImplementationConfiguration, ITelemetryFactory<ITelemetryService, ITelemetryImplementationConfiguration>>),
    typeof(ITelemetryType),
    typeof(TelemetryTypes),
    ServiceInterface = typeof(ITelemetryService),
    ProviderType = typeof(TelemetryServiceProvider),
    ProviderInterface = typeof(ITelemetryServiceProvider),
    ServiceCategory = "Telemetry")]
public partial class TelemetryTypes : ServiceTypeCollectionBase<
    TelemetryTypeBase<ITelemetryService, ITelemetryImplementationConfiguration, ITelemetryFactory<ITelemetryService, ITelemetryImplementationConfiguration>>,
    ITelemetryType>
{
    /// <summary>The connection this domain's configuration rows are read from and written to.</summary>
    public static string ConfigurationConnection { get; set; } = "ServerConfiguration";

    static TelemetryTypes()
    {
        var collectOptions = RegisterFunc;

        var providerService = typeof(ITelemetryServiceProvider).ToString();

        Registration((builder, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<TelemetryTypes>() ?? NullLogger<TelemetryTypes>.Instance;

            var registered = collectOptions(builder, loggerFactory);
            if (registered.IsFailure)
                return registered;

            builder.Services.TryAddSingleton<ITelemetryConfigurationProvider>(sp =>
                new TelemetryConfigurationProvider(
                    sp.GetService<ILogger<TelemetryConfigurationProvider>>()!,
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    ConfigurationConnection));
            builder.Services.TryAddSingleton<TelemetryConfigurationProvider>(
                sp => (TelemetryConfigurationProvider)sp.GetRequiredService<ITelemetryConfigurationProvider>());

            var declaredOptions = Options;
            var optionNames = string.Join(", ", declaredOptions.Select(option => option.Name));

            ServiceTypeLog.DomainOptionsCollected(log, nameof(TelemetryTypes), declaredOptions.Length, optionNames);
            ServiceTypeLog.DomainProviderDeclared(log, nameof(TelemetryTypes), providerService);

            builder.Services.AddScoped<ITelemetryServiceProvider>(sp =>
            {
                var provider = new TelemetryServiceProvider(
                    sp,
                    sp.GetService<ILogger<TelemetryServiceProvider>>()
                    ?? NullLogger<TelemetryServiceProvider>.Instance);

                var stLogger = sp.GetService<ILoggerFactory>()?.CreateLogger<TelemetryTypes>()
                    ?? NullLogger<TelemetryTypes>.Instance;
                ServiceTypeLog.DomainProviderConstructing(stLogger, nameof(TelemetryTypes), provider.GetType().Name);
                if (sp.GetService<ITelemetryConfigurationProvider>() is { } cfgProvider)
                {
                    var domainResult = provider.Register(cfgProvider);
                    if (domainResult.IsSuccess)
                        ServiceTypeLog.DomainConfigurationSourceAttached(stLogger, nameof(TelemetryTypes), provider.GetType().Name, cfgProvider.GetType().Name);
                    else
                        ServiceTypeLog.DomainConfigurationSourceRejected(stLogger, nameof(TelemetryTypes), provider.GetType().Name, cfgProvider.GetType().Name, domainResult.CurrentMessage);
                }
                else
                {
                    ServiceTypeLog.DomainHasNoConfigurationSource(
                        stLogger,
                        nameof(TelemetryTypes),
                        provider.GetType().Name,
                        typeof(IServiceConfigurationProvider<TelemetryConfiguration>).ToString());
                }

                return provider;
            });

            if (declaredOptions.Length == 0)
                ServiceTypeLog.DomainRegisteredWithNoOptions(log, nameof(TelemetryTypes), providerService);
            else
                ServiceTypeLog.DomainRegistered(log, nameof(TelemetryTypes), declaredOptions.Length, optionNames, providerService);

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
