using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>MessageLogging for searching the distinct values of a container column (FDW-5666).</summary>
/// <remarks>
/// One method and one code per reason. The number is the code and its first digit the category: 1 progress, 2 a request the
/// service refuses (400), 3 a column the container does not have (404), 6 a setting the host declared wrongly, 7 a container
/// or a read the service could not reach (the reach's own failure is answered, so its code says why), 9 an answer the store
/// should never give.
/// </remarks>
[MessageLoggingTypeCode("DATA")]
public static partial class ColumnValuesLog
{
    /// <summary>Logs that a column's values are being searched.</summary>
    [MessageLogging(EventId = 11660, Level = LogLevel.Trace, Message = "Searching the values of column '{column}' of container '{container}' in data store '{dataStore}' (skip {skip}, take {take})")]
    public static partial IGenericMessage Searching(ILogger logger, string column, string container, string dataStore, int skip, int take);

    /// <summary>Logs how many values a search answered.</summary>
    [MessageLogging(EventId = 11661, Level = LogLevel.Debug, Message = "Answered {count} values of column '{column}' of container '{container}', more: {hasMore}")]
    public static partial IGenericMessage Searched(ILogger logger, int count, string column, string container, bool hasMore);

    /// <summary>Logs that the column values search was registered, with the page sizes the option declares.</summary>
    [MessageLogging(EventId = 11662, Level = LogLevel.Trace, Message = "Registered the column values search: default take {defaultTake}, maximum take {maximumTake}")]
    public static partial IGenericMessage Registered(ILogger logger, int defaultTake, int maximumTake);

    /// <summary>A take above the maximum the Main data gateway option declares.</summary>
    [MessageLogging(EventId = 21660, Level = LogLevel.Warning, Message = "A page of {take} values was asked for; at most {maximum} are answered at once")]
    public static partial IGenericMessage TakeAboveMaximum(ILogger logger, int take, int maximum);

    /// <summary>A take of zero or less.</summary>
    [MessageLogging(EventId = 21661, Level = LogLevel.Warning, Message = "A page of {take} values was asked for; a page holds at least one")]
    public static partial IGenericMessage TakeNotPositive(ILogger logger, int take);

    /// <summary>A negative skip.</summary>
    [MessageLogging(EventId = 21662, Level = LogLevel.Warning, Message = "{skip} values were asked to be passed over; a skip is zero or more")]
    public static partial IGenericMessage SkipNegative(ILogger logger, int skip);

    /// <summary>A chosen value that is null or empty.</summary>
    [MessageLogging(EventId = 21663, Level = LogLevel.Warning, Message = "A chosen value of column '{column}' is empty; a chosen value is the text of a value")]
    public static partial IGenericMessage ChosenValueEmpty(ILogger logger, string column);

    /// <summary>A column that is not one of the container's discovered fields.</summary>
    [MessageLogging(EventId = 31660, Level = LogLevel.Warning, Message = "Container '{container}' of data store '{dataStore}' has no discovered field '{column}'")]
    public static partial IGenericMessage ColumnNotDiscovered(ILogger logger, string column, string container, string dataStore);

    /// <summary>The page settings the Main data gateway option declares do not make a page.</summary>
    [MessageLogging(EventId = 61660, Level = LogLevel.Critical, Message = "The column values page settings are not usable: default take {defaultTake}, maximum take {maximumTake}; both must be at least one and the default no more than the maximum")]
    public static partial IGenericMessage PagingSettingsInvalid(ILogger logger, int defaultTake, int maximumTake);

    /// <summary>The container could not be reached; its own failure is answered.</summary>
    [MessageLogging(EventId = 71660, Level = LogLevel.Warning, Message = "Container '{container}' under path '{path}' of data store '{dataStore}' could not be reached to search its values")]
    public static partial IGenericMessage ContainerUnreachable(ILogger logger, string container, string path, string dataStore);

    /// <summary>The grouped read was refused or failed; its own failure is answered.</summary>
    [MessageLogging(EventId = 71661, Level = LogLevel.Warning, Message = "The values of column '{column}' of container '{container}' in data store '{dataStore}' could not be read")]
    public static partial IGenericMessage ValuesUnreadable(ILogger logger, string column, string container, string dataStore);

    /// <summary>The store answered a row whose count is not a whole number.</summary>
    [MessageLogging(EventId = 91660, Level = LogLevel.Error, Message = "The store answered a count for a value of column '{column}' that is not a whole number")]
    public static partial IGenericMessage RowCountNotWhole(ILogger logger, string column);
}
