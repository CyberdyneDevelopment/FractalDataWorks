using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Telemetry.Abstractions;

namespace Fdw.Services.Telemetry;

/// <summary>
/// The telemetry domain's service provider.
/// </summary>
public sealed class TelemetryServiceProvider
    : PlatformServiceProviderBase<
        ITelemetryService,
        ITelemetryImplementationConfiguration,
        ITelemetryFactory<ITelemetryService, ITelemetryImplementationConfiguration>,
        ITelemetryConfigurationProvider>,
      ITelemetryServiceProvider
{
    /// <summary>Initializes a new instance of the <see cref="TelemetryServiceProvider"/> class.</summary>
    /// <param name="services">The container this provider resolves its factories from.</param>
    /// <param name="logger">The logger for this provider.</param>
    public TelemetryServiceProvider(IServiceProvider services, ILogger<TelemetryServiceProvider> logger)
        : base(services, logger ?? NullLogger<TelemetryServiceProvider>.Instance)
    {
    }
}
