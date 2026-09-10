using Fdw.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Telemetry.Abstractions;
using Fdw.Services.Telemetry.Commands;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Telemetry;

/// <summary>Supplies the Telemetry configuration.</summary>
public sealed class TelemetryConfigurationProvider
    : DomainConfigurationProviderBase<ITelemetryImplementationConfiguration>,
      ITelemetryConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="TelemetryConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public TelemetryConfigurationProvider(
        ILogger<TelemetryConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "otel", "Telemetry")
    {
    }
}
