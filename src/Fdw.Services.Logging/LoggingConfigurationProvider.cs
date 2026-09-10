using Fdw.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Logging.Abstractions;
using Fdw.Services.Logging.Commands;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Logging;

/// <summary>Supplies the Logging domain configuration.</summary>
public sealed class LoggingConfigurationProvider
    : DomainConfigurationProviderBase<ILoggingImplementationConfiguration>,
      ILoggingConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="LoggingConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public LoggingConfigurationProvider(
        ILogger<LoggingConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "log", "Logging")
    {
    }
}
