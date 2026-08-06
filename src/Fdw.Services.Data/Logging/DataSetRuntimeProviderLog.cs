using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// Source-generated logging methods for <see cref="DataSetRuntimeProvider"/>.
/// EventId range: 5420-5439.
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class DataSetRuntimeProviderLog
{
    /// <summary>Traces entry into DataSetRuntimeProvider.Get by name.</summary>
    [MessageLogging(EventId = 11091, Level = LogLevel.Trace,
        Message = "DataSetRuntimeProvider resolving DataSet '{dataSetName}'")]
    public static partial IGenericMessage TraceGet(ILogger logger, string dataSetName);

    /// <summary>Traces entry into DataSetRuntimeProvider.Get by ID.</summary>
    [MessageLogging(EventId = 11092, Level = LogLevel.Trace,
        Message = "DataSetRuntimeProvider resolving DataSet by ID '{id}'")]
    public static partial IGenericMessage TraceGetById(ILogger logger, Guid id);

    /// <summary>Traces entry into DataSetRuntimeProvider.Get (all).</summary>
    [MessageLogging(EventId = 11093, Level = LogLevel.Trace,
        Message = "DataSetRuntimeProvider loading all DataSet runtimes")]
    public static partial IGenericMessage TraceGetAll(ILogger logger);

    /// <summary>Logs successful resolution of a DataSet by name.</summary>
    [MessageLogging(EventId = 11094, Level = LogLevel.Information,
        Message = "DataSet '{dataSetName}' runtime resolved")]
    public static partial IGenericMessage Retrieved(ILogger logger, string dataSetName);

    /// <summary>Logs successful resolution of a DataSet by ID.</summary>
    [MessageLogging(EventId = 11095, Level = LogLevel.Information,
        Message = "DataSet by ID '{id}' runtime resolved")]
    public static partial IGenericMessage RetrievedById(ILogger logger, Guid id);

    /// <summary>Logs successful resolution of all DataSet runtimes.</summary>
    [MessageLogging(EventId = 11096, Level = LogLevel.Information,
        Message = "DataSetRuntimeProvider loaded {count} DataSet runtimes")]
    public static partial IGenericMessage AllRetrieved(ILogger logger, int count);

    /// <summary>Logs when configuration is not found by name.</summary>
    [MessageLogging(EventId = 31017, Level = LogLevel.Warning,
        Message = "DataSet configuration '{dataSetName}' not found")]
    public static partial IGenericMessage ConfigurationNotFound(ILogger logger, string dataSetName);

    /// <summary>Logs when configuration is not found by ID.</summary>
    [MessageLogging(EventId = 31018, Level = LogLevel.Warning,
        Message = "DataSet configuration with ID '{id}' not found")]
    public static partial IGenericMessage ConfigurationNotFoundById(ILogger logger, Guid id);

    /// <summary>Logs failure to build a DataSet runtime from its configuration.</summary>
    [MessageLogging(EventId = 91012, Level = LogLevel.Error,
        Message = "Failed to build DataSet runtime for '{dataSetName}'")]
    public static partial IGenericMessage BuildFailed(ILogger logger, string dataSetName);

    /// <summary>Logs failure to load all DataSet configurations.</summary>
    [MessageLogging(EventId = 71020, Level = LogLevel.Error,
        Message = "DataSetRuntimeProvider failed to load all DataSet configurations")]
    public static partial IGenericMessage AllConfigsLoadFailed(ILogger logger);

    /// <summary>Logs when some DataSet runtimes could not be built during a GetAll operation.</summary>
    [MessageLogging(EventId = 91013, Level = LogLevel.Warning,
        Message = "DataSetRuntimeProvider: {failureCount} of {totalCount} DataSet runtime builds failed")]
    public static partial IGenericMessage SomeBuildsFailed(ILogger logger, int failureCount, int totalCount);
}
