using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// MessageLogging for DataSetTypes operations.
/// EventId range: 5110-5119
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class DataSetTypesLog
{
    /// <summary>
    /// Logs that DataSet configuration entries were skipped due to empty names.
    /// This can occur when IOptions binds partial entries from configuration keys
    /// that don't include all required properties.
    /// </summary>
    [MessageLogging(
        EventId = 21003,
        Level = LogLevel.Warning,
        Message = "Skipped {skippedCount} DataSet configuration entries with empty Name (IOptions binding created {totalCount} entries, {validCount} valid)")]
    public static partial IGenericMessage DataSetConfigurationEntriesSkipped(ILogger logger, int skippedCount, int totalCount, int validCount);

    /// <summary>
    /// Logs DataSetTypes initialization completion.
    /// </summary>
    [MessageLogging(
        EventId = 11105,
        Level = LogLevel.Information,
        Message = "Initialized {typeCount} DataSet types and {configCount} configured DataSets")]
    public static partial IGenericMessage DataSetTypesInitialized(ILogger logger, int typeCount, int configCount);

    /// <summary>
    /// Logs DataSetTypes initialization when no configurations are present.
    /// </summary>
    [MessageLogging(
        EventId = 11106,
        Level = LogLevel.Information,
        Message = "Initialized {typeCount} DataSet types")]
    public static partial IGenericMessage DataSetTypesInitializedNoConfig(ILogger logger, int typeCount);

    /// <summary>
    /// Logs registration of a configured DataSet.
    /// </summary>
    [MessageLogging(
        EventId = 11107,
        Level = LogLevel.Debug,
        Message = "Registered configured DataSet '{name}'")]
    public static partial IGenericMessage ConfiguredDataSetRegistered(ILogger logger, string name);

    /// <summary>
    /// Logs how many DataSet configurations were registered from IOptions.
    /// </summary>
    [MessageLogging(
        EventId = 11108,
        Level = LogLevel.Information,
        Message = "Registered {count} DataSet type(s) from configuration")]
    public static partial IGenericMessage DataSetTypesRegisteredFromConfig(ILogger logger, int count);
}
