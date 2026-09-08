using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Logging.Abstractions;
using Fdw.Services.Logging.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Logging;

/// <summary>
/// Supplies logging configuration, composing the domain record with the implementation's own.
/// </summary>
/// <remarks>
/// Why <c>ServerConfiguration</c> rather than <c>PlatformConfiguration</c>: a logging pipeline has to
/// exist before the platform store is reachable, so this domain's rows live in the file-backed server
/// tier declared in <c>configurationSchema.json</c>. The gateway is the ordinary one — only the
/// datastore differs.
/// </remarks>
public class LoggingConfigurationProvider
    : ServiceConfigurationProviderBase<
          LoggingConfiguration,
          ILoggingImplementationConfiguration,
          LoggingConfigurationCommand>,
      ILoggingConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="LoggingConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger for this provider.</param>
    /// <param name="gatewayProvider">Yields the gateway for the named datastore.</param>
    /// <param name="dataStoreName">The datastore this reads through — the server tier.</param>
    /// <param name="pathName">The path holding the logging tables.</param>
    public LoggingConfigurationProvider(
        ILogger<LoggingConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName = "ServerConfiguration",
        string pathName = "log")
        : base(logger ?? NullLogger<LoggingConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
    }

    /// <inheritdoc />
    protected override LoggingConfiguration Compose<T>(
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
