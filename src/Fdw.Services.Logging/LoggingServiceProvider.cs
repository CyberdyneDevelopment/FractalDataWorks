using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Logging.Abstractions;

namespace Fdw.Services.Logging;

/// <summary>
/// The logging domain's service provider.
/// </summary>
public sealed class LoggingServiceProvider
    : PlatformServiceProviderBase<
        ILoggingService,
        ILoggingImplementationConfiguration,
        ILoggingFactory<ILoggingService, ILoggingImplementationConfiguration>,
        ILoggingConfigurationProvider>,
      ILoggingServiceProvider
{
    /// <summary>Initializes a new instance of the <see cref="LoggingServiceProvider"/> class.</summary>
    /// <param name="services">The container this provider resolves its factories from.</param>
    /// <param name="logger">The logger for this provider.</param>
    public LoggingServiceProvider(IServiceProvider services, ILogger<LoggingServiceProvider> logger)
        : base(services, logger ?? NullLogger<LoggingServiceProvider>.Instance)
    {
    }
}
