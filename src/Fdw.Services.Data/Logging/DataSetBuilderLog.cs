using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// Source-generated logging methods for <see cref="DataSetBuilder"/>.
/// EventId range: 5400-5419.
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class DataSetBuilderLog
{
    /// <summary>Traces entry into DataSetBuilder.Create.</summary>
    [MessageLogging(EventId = 11064, Level = LogLevel.Trace,
        Message = "DataSetBuilder creating runtime for DataSet '{dataSetName}'")]
    public static partial IGenericMessage TraceCreate(ILogger logger, string dataSetName);

    /// <summary>Build was called before Configure supplied a configuration.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 21010, Level = LogLevel.Error,
        Message = "DataSetBuilder.Build was called before Configure — a builder has nothing to build until it is given a configuration")]
    public static partial IGenericMessage NotConfigured(ILogger logger);

    /// <summary>Logs successful creation of a DataSet runtime.</summary>
    [MessageLogging(EventId = 11065, Level = LogLevel.Information,
        Message = "DataSet '{dataSetName}' runtime created: {sourceCount} sources, {fieldCount} fields")]
    public static partial IGenericMessage Created(ILogger logger, string dataSetName, int sourceCount, int fieldCount);

    /// <summary>Logs failure when the configuration name is missing.</summary>
    [MessageLogging(EventId = 21002, Level = LogLevel.Error,
        Message = "DataSetBuilder.Create failed: DataSetConfiguration.Name is required")]
    public static partial IGenericMessage ConfigurationNameRequired(ILogger logger);

    /// <summary>Logs failure when source resolution fails during dataset construction.</summary>
    [MessageLogging(EventId = 91008, Level = LogLevel.Error,
        Message = "DataSetBuilder.Create failed for '{dataSetName}': source resolution returned failure")]
    public static partial IGenericMessage SourceResolutionFailed(ILogger logger, string dataSetName);

    /// <summary>Logs failure when a join cannot be built from configuration.</summary>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "DataSetBuilder.Create failed for '{dataSetName}': join build error — {reason}")]
    public static partial IGenericMessage JoinBuildFailed(ILogger logger, string dataSetName, string reason);
}
