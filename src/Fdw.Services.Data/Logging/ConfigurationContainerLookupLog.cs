using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// MessageLogging for ConfigurationContainerLookup operations.
/// EventId range: 5580-5589
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class ConfigurationContainerLookupLog
{
    /// <summary>Logs the start of a container lookup by name.</summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Trace,
        Message = "ConfigurationContainerLookup: looking up container '{configTypeName}'")]
    public static partial IGenericMessage LookupStarted(ILogger logger, string configTypeName);

    /// <summary>Logs when a container is not found in any DataStore path.</summary>
    [MessageLogging(EventId = 31003, Level = LogLevel.Error,
        Message = "ConfigurationContainerLookup: container '{configTypeName}' not found in any DataStore path")]
    public static partial IGenericMessage ContainerNotFound(ILogger logger, string configTypeName);

    /// <summary>Logs when a container is resolved successfully.</summary>
    [MessageLogging(EventId = 11010, Level = LogLevel.Trace,
        Message = "ConfigurationContainerLookup: container '{configTypeName}' resolved in store '{storeName}' path '{pathName}'")]
    public static partial IGenericMessage ContainerResolved(ILogger logger, string configTypeName, string storeName, string pathName);

    /// <summary>Logs the result count of a ByCategory query.</summary>
    [MessageLogging(EventId = 11011, Level = LogLevel.Trace,
        Message = "ConfigurationContainerLookup: ByCategory('{sectionPath}') — returning {count} container(s)")]
    public static partial IGenericMessage ByCategoryResult(ILogger logger, string sectionPath, int count);

    /// <summary>Logs the result count of an All() query.</summary>
    [MessageLogging(EventId = 11012, Level = LogLevel.Trace,
        Message = "ConfigurationContainerLookup: All() — returning {count} container(s)")]
    public static partial IGenericMessage AllResult(ILogger logger, int count);
}
