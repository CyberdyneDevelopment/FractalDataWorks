using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.Services.Telemetry.Abstractions;
using Fdw.ServiceTypes;

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
}
