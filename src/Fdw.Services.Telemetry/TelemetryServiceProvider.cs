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
    : DomainServiceProviderBase<
        ITelemetryService,
        ITelemetryImplementationConfiguration,
        ITelemetryFactory<ITelemetryService, ITelemetryImplementationConfiguration>,
        ITelemetryConfigurationProvider>,
      ITelemetryServiceProvider
{
    /// <summary>Initializes a new instance of the <see cref="TelemetryServiceProvider"/> class.</summary>
    /// <param name="logger">The logger for this provider.</param>
    public TelemetryServiceProvider(ILogger<TelemetryServiceProvider> logger)
        : base(logger ?? NullLogger<TelemetryServiceProvider>.Instance)
    {
    }
}
