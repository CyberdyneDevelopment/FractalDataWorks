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
    : DomainServiceProviderBase<
        ILoggingService,
        ILoggingImplementationConfiguration,
        ILoggingFactory<ILoggingService, ILoggingImplementationConfiguration>,
        ILoggingConfigurationProvider>,
      ILoggingServiceProvider
{
    /// <summary>Initializes a new instance of the <see cref="LoggingServiceProvider"/> class.</summary>
    /// <param name="logger">The logger for this provider.</param>
    public LoggingServiceProvider(ILogger<LoggingServiceProvider> logger)
        : base(logger ?? NullLogger<LoggingServiceProvider>.Instance)
    {
    }
}
