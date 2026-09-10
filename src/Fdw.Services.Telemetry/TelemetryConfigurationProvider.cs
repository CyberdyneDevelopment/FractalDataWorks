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
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public TelemetryConfigurationProvider(
        ILogger<TelemetryConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "otel", "Telemetry")
    {
    }
}
