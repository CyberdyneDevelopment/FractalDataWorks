using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Telemetry.Abstractions;
using Fdw.Services.Telemetry.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Telemetry;

/// <summary>
/// Supplies telemetry configuration, composing the domain record with the implementation's own.
/// </summary>
/// <remarks>
/// Why <c>ServerConfiguration</c> rather than <c>PlatformConfiguration</c>: a telemetry pipeline has to
/// exist before the platform store is reachable, so this domain's rows live in the file-backed server
/// tier declared in <c>configurationSchema.json</c>. The gateway is the ordinary one — only the
/// datastore differs.
/// </remarks>
public class TelemetryConfigurationProvider
    : ServiceConfigurationProviderBase<
          TelemetryConfiguration,
          ITelemetryImplementationConfiguration,
          TelemetryConfigurationCommand>,
      ITelemetryConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="TelemetryConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger for this provider.</param>
    /// <param name="gatewayProvider">Yields the gateway for the named datastore.</param>
    /// <param name="dataStoreName">The datastore this reads through — the server tier.</param>
    /// <param name="pathName">The path holding the telemetry tables.</param>
    public TelemetryConfigurationProvider(
        ILogger<TelemetryConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName = "ServerConfiguration",
        string pathName = "otel")
        : base(logger ?? NullLogger<TelemetryConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
    }

    /// <inheritdoc />
    protected override TelemetryConfiguration Compose<T>(
        string serviceOptionType,
        string name,
        T implementationConfiguration)
        => new()
        {
            Name = name,
            ServiceOptionType = serviceOptionType,
            Configuration = implementationConfiguration,
        };
}
