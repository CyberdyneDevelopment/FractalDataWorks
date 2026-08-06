using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.UI.Components.Logging;

/// <summary>
/// Structured logging for DataPreviewPageProvider.
/// EventId range: 1750-1759
/// </summary>
[MessageLoggingTypeCode("UICOMPONENTS")]
public static partial class DataPreviewPageLog
{
    [MessageLogging(EventId = 11005, Level = LogLevel.Trace, Message = "DataStore changed to '{dataStore}' — rebuilding container picker")]
    public static partial IGenericMessage DataStoreChanged(ILogger logger, string dataStore);

    [MessageLogging(EventId = 11006, Level = LogLevel.Trace, Message = "Preview executed for DataStore '{dataStore}', path '{path}', container '{container}'")]
    public static partial IGenericMessage PreviewExecuted(ILogger logger, string dataStore, string path, string container);

    [MessageLogging(EventId = 11007, Level = LogLevel.Trace, Message = "Exporting {rowCount} rows as CSV")]
    public static partial IGenericMessage ExportingCsv(ILogger logger, int rowCount);

    [MessageLogging(EventId = 11008, Level = LogLevel.Trace, Message = "Session state restored for data preview")]
    public static partial IGenericMessage SessionStateRestored(ILogger logger);

    [MessageLogging(EventId = 71001, Level = LogLevel.Error, Message = "Failed to load DataStore list for preview picker")]
    public static partial IGenericMessage LoadDataStoresFailed(ILogger logger, System.Exception exception);

    [MessageLogging(EventId = 11009, Level = LogLevel.Information, Message = "Loaded {count} DataStores for preview picker")]
    public static partial IGenericMessage DataStoresLoaded(ILogger logger, int count);
}
