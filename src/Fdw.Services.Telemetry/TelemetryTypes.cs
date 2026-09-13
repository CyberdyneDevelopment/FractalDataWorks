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

        Registration((builder, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<TelemetryTypes>() ?? NullLogger<TelemetryTypes>.Instance;

            // The domain's own provider goes in BEFORE its options are collected, because an
            // option's Register exists to hand this provider an implementation and cannot do that
            // against a provider the container does not have yet. Collecting first left every
            // option registering against nothing.
            builder.Services.TryAddSingleton<ITelemetryConfigurationProvider>(sp =>
                new TelemetryConfigurationProvider(
                    sp.GetService<ILogger<TelemetryConfigurationProvider>>()!,
                    sp.GetRequiredService<IConfigurationGatewayProvider>(), TelemetryTypes.ConfigurationConnection));
            builder.Services.TryAddSingleton<TelemetryConfigurationProvider>(
                sp => (TelemetryConfigurationProvider)sp.GetRequiredService<ITelemetryConfigurationProvider>());

            var registered = collectOptions(builder, loggerFactory);
            if (registered.IsFailure)
                return registered;

            var declaredOptions = Options;
            var optionNames = string.Join(", ", declaredOptions.Select(option => option.Name));

            ServiceTypeLog.DomainOptionsCollected(log, nameof(TelemetryTypes), declaredOptions.Length, optionNames);

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
