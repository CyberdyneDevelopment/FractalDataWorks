using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.Services.Logging.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Logging;

/// <summary>
/// The logging option set — one option per logging implementation.
/// </summary>
/// <remarks>
/// Logging is the earliest domain to come up, so its configuration is read through the
/// <c>ServerConfiguration</c> connection declared in <c>configurationSchema.json</c> rather than
/// <c>PlatformConfiguration</c>: a logging pipeline has to exist before the platform store is
/// reachable.
/// </remarks>
[ServiceTypeCollection(
    typeof(LoggingTypeBase<ILoggingService, ILoggingImplementationConfiguration, ILoggingFactory<ILoggingService, ILoggingImplementationConfiguration>>),
    typeof(ILoggingType),
    typeof(LoggingTypes),
    ServiceInterface = typeof(ILoggingService),
    ProviderType = typeof(LoggingServiceProvider),
    ProviderInterface = typeof(ILoggingServiceProvider),
    ServiceCategory = "Logging")]
public partial class LoggingTypes : ServiceTypeCollectionBase<
    LoggingTypeBase<ILoggingService, ILoggingImplementationConfiguration, ILoggingFactory<ILoggingService, ILoggingImplementationConfiguration>>,
    ILoggingType>
{
}
