using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.DataSets;

/// <summary>
/// MessageLogging for DataSet source attach/detach operations.
/// </summary>
/// <remarks>
/// Why: each condition here shares its EventId with the paired <c>DataSetsResultCodeBase</c>
/// TypeOption in <c>Fdw.Data.DataSets.Results</c> (same number, same meaning) — see
/// <see cref="Fdw.Data.DataSets.Results.DataSetsResultCodeBase"/>.
/// </remarks>
[MessageLoggingTypeCode("DATASETS")]
public static partial class DataSetSourceLog
{
    /// <summary>Traces entry into a DataSet source attach operation.</summary>
    [MessageLogging(EventId = 11100, Level = LogLevel.Trace,
        Message = "[DataSetSource] Attaching source '{sourceName}' to DataSet '{dataSetName}'")]
    public static partial IGenericMessage AttachingSource(ILogger logger, string sourceName, string dataSetName);

    /// <summary>Logs successful DataSet source attachment.</summary>
    [MessageLogging(EventId = 11101, Level = LogLevel.Information,
        Message = "[DataSetSource] Attached source '{sourceName}' to DataSet '{dataSetName}'")]
    public static partial IGenericMessage SourceAttached(ILogger logger, string sourceName, string dataSetName);

    /// <summary>Traces entry into a DataSet source detach operation.</summary>
    [MessageLogging(EventId = 11102, Level = LogLevel.Trace,
        Message = "[DataSetSource] Detaching source '{sourceId}'")]
    public static partial IGenericMessage DetachingSource(ILogger logger, Guid sourceId);

    /// <summary>Logs successful DataSet source detachment.</summary>
    [MessageLogging(EventId = 11103, Level = LogLevel.Information,
        Message = "[DataSetSource] Detached source '{sourceId}'")]
    public static partial IGenericMessage SourceDetached(ILogger logger, Guid sourceId);

    /// <summary>Logs that a source names neither a container nor a source DataSet as its target.</summary>
    [MessageLogging(EventId = 21100, Level = LogLevel.Warning,
        Message = "[DataSetSource] Source '{sourceName}' on DataSet '{dataSetName}' names neither a container nor a source DataSet as its target")]
    public static partial IGenericMessage SourceMissingTarget(ILogger logger, string sourceName, string dataSetName);

    /// <summary>Logs that the source DataSet referenced as a source's target was not found.</summary>
    [MessageLogging(EventId = 31100, Level = LogLevel.Warning,
        Message = "[DataSetSource] Source DataSet '{sourceDataSetName}' referenced by DataSet '{dataSetName}' was not found")]
    public static partial IGenericMessage SourceDataSetNotFound(ILogger logger, string sourceDataSetName, string dataSetName);

    /// <summary>Logs that the container referenced as a source's target was not found.</summary>
    [MessageLogging(EventId = 31101, Level = LogLevel.Warning,
        Message = "[DataSetSource] Source container '{containerName}' referenced by DataSet '{dataSetName}' was not found")]
    public static partial IGenericMessage SourceContainerNotFound(ILogger logger, string containerName, string dataSetName);

    /// <summary>Logs that a source with the given name is already attached to the DataSet.</summary>
    [MessageLogging(EventId = 41100, Level = LogLevel.Warning,
        Message = "[DataSetSource] Source '{sourceName}' is already attached to DataSet '{dataSetName}'")]
    public static partial IGenericMessage SourceAlreadyAttached(ILogger logger, string sourceName, string dataSetName);

    /// <summary>Logs that detaching a source from a DataSet failed.</summary>
    [MessageLogging(EventId = 91100, Level = LogLevel.Error,
        Message = "[DataSetSource] Failed to detach source '{sourceName}' from DataSet '{dataSetName}': {reason}")]
    public static partial IGenericMessage SourceDetachFailed(ILogger logger, string sourceName, string dataSetName, string reason);
}
