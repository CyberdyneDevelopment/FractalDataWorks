using System;
using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>MessageLogging for the option rows of a data store container (FDW-5664).</summary>
/// <remarks>
/// One method and one code per reason. The first digit (two for 12) is the category: 1 progress, 2 a request refused as it
/// stands (400), 3 a column, row or id that is not there (404), 4 a pair already stored (409), 7 an options table or write the
/// service could not reach (its own failure is answered), 9 a stored row the service cannot read, 12 an options table no
/// setting names (409, refused because of the host's state). A refusal of an option type or option is DATAOPTION-31000 or
/// 30000, from <c>DataOptionLog</c>.
/// </remarks>
[MessageLoggingTypeCode("DATA")]
public static partial class DataOptionRowsLog
{
    /// <summary>Logs that a container's option rows are being listed.</summary>
    [MessageLogging(EventId = 11670, Level = LogLevel.Trace, Message = "Listing the option rows of container '{container}' in data store '{dataStore}' (column '{column}', skip {skip}, take {take})")]
    public static partial IGenericMessage Listing(ILogger logger, string container, string dataStore, string column, int skip, int take);

    /// <summary>Logs how many rows a list answered.</summary>
    [MessageLogging(EventId = 11671, Level = LogLevel.Debug, Message = "Answered {count} option rows of container '{container}', more: {hasMore}")]
    public static partial IGenericMessage Listed(ILogger logger, int count, string container, bool hasMore);

    /// <summary>Logs how many rows an add wrote.</summary>
    [MessageLogging(EventId = 11672, Level = LogLevel.Information, Message = "Added {count} option rows to container '{container}' of data store '{dataStore}'")]
    public static partial IGenericMessage Added(ILogger logger, int count, string container, string dataStore);

    /// <summary>Logs that a row was changed.</summary>
    [MessageLogging(EventId = 11673, Level = LogLevel.Information, Message = "Changed option row {id} of container '{container}'")]
    public static partial IGenericMessage Changed(ILogger logger, Guid id, string container);

    /// <summary>Logs how many rows a remove deleted.</summary>
    [MessageLogging(EventId = 11674, Level = LogLevel.Information, Message = "Removed {count} option rows of container '{container}'")]
    public static partial IGenericMessage Removed(ILogger logger, int count, string container);

    /// <summary>Logs that the option rows service was registered.</summary>
    [MessageLogging(EventId = 11675, Level = LogLevel.Trace, Message = "Registered the option rows service")]
    public static partial IGenericMessage Registered(ILogger logger);

    /// <summary>An add with no rows.</summary>
    [MessageLogging(EventId = 21670, Level = LogLevel.Warning, Message = "An add for container '{container}' names no rows; it names at least one")]
    public static partial IGenericMessage NoRowsToAdd(ILogger logger, string container);

    /// <summary>A row to add whose value is empty.</summary>
    [MessageLogging(EventId = 21671, Level = LogLevel.Warning, Message = "Row {position} of the add has no value; an option row applies to a value")]
    public static partial IGenericMessage ValueEmpty(ILogger logger, int position);

    /// <summary>A column and value an add names twice.</summary>
    [MessageLogging(EventId = 21672, Level = LogLevel.Warning, Message = "Row {position} of the add names column '{column}' and value '{value}' again; a column has one row for a value")]
    public static partial IGenericMessage PairTwiceInAdd(ILogger logger, int position, string column, string value);

    /// <summary>A change that states nothing to change.</summary>
    [MessageLogging(EventId = 21673, Level = LogLevel.Warning, Message = "The change of option row {id} states no option, active or value")]
    public static partial IGenericMessage NothingToChange(ILogger logger, Guid id);

    /// <summary>A change to an empty value.</summary>
    [MessageLogging(EventId = 21674, Level = LogLevel.Warning, Message = "The change of option row {id} states an empty value; an option row applies to a value")]
    public static partial IGenericMessage NewValueEmpty(ILogger logger, Guid id);

    /// <summary>A remove with no ids.</summary>
    [MessageLogging(EventId = 21675, Level = LogLevel.Warning, Message = "A remove for container '{container}' names no ids; it names at least one")]
    public static partial IGenericMessage NoIdsToRemove(ILogger logger, string container);

    /// <summary>A row to add that does not say whether it is active.</summary>
    [MessageLogging(EventId = 21677, Level = LogLevel.Warning, Message = "Row {position} of the add does not say whether it is active")]
    public static partial IGenericMessage ActiveNotStated(ILogger logger, int position);

    /// <summary>Says which row of an add a refusal is about; it follows the refusal's own message.</summary>
    [MessageLogging(EventId = 21676, Level = LogLevel.Warning, Message = "Row {position} of the add is refused, so no row is added")]
    public static partial IGenericMessage RowRefused(ILogger logger, int position);

    /// <summary>A column that is not one of the container's discovered fields.</summary>
    [MessageLogging(EventId = 31670, Level = LogLevel.Warning, Message = "Container '{container}' of data store '{dataStore}' has no discovered field '{column}'")]
    public static partial IGenericMessage ColumnNotDiscovered(ILogger logger, string column, string container, string dataStore);

    /// <summary>A row id the container does not have.</summary>
    [MessageLogging(EventId = 31671, Level = LogLevel.Warning, Message = "Container '{container}' has no option row {id}")]
    public static partial IGenericMessage RowNotFound(ILogger logger, Guid id, string container);

    /// <summary>Ids a remove names that are not the container's rows.</summary>
    [MessageLogging(EventId = 31672, Level = LogLevel.Warning, Message = "Container '{container}' has no option rows {ids}; nothing is removed")]
    public static partial IGenericMessage IdsNotInContainer(ILogger logger, string ids, string container);

    /// <summary>A column and value the container already has a row for.</summary>
    [MessageLogging(EventId = 41670, Level = LogLevel.Warning, Message = "Container '{container}' already has an option row for column '{column}' and value '{value}'")]
    public static partial IGenericMessage PairAlreadyStored(ILogger logger, string column, string value, string container);

    /// <summary>The options table could not be reached; its own failure is answered.</summary>
    [MessageLogging(EventId = 71670, Level = LogLevel.Warning, Message = "The options table '{container}' under path '{path}' of data store '{dataStore}' could not be read")]
    public static partial IGenericMessage OptionsTableUnreadable(ILogger logger, string container, string path, string dataStore);

    /// <summary>A write the store refused; its own failure is answered.</summary>
    [MessageLogging(EventId = 71671, Level = LogLevel.Warning, Message = "The options table '{container}' of data store '{dataStore}' refused the {operation}")]
    public static partial IGenericMessage OptionsTableRefusedWrite(ILogger logger, string operation, string container, string dataStore);

    /// <summary>The container whose column a row names could not be reached; its own failure is answered.</summary>
    [MessageLogging(EventId = 71672, Level = LogLevel.Warning, Message = "Container '{container}' under path '{path}' of data store '{dataStore}' could not be reached to check its fields")]
    public static partial IGenericMessage ContainerUnreachable(ILogger logger, string container, string path, string dataStore);

    /// <summary>A stored row that lacks a field or holds one of the wrong kind.</summary>
    [MessageLogging(EventId = 91670, Level = LogLevel.Error, Message = "A row of the options table has no readable '{field}'")]
    public static partial IGenericMessage StoredRowUnreadable(ILogger logger, string field);

    /// <summary>A server setting naming the options table that is not set.</summary>
    [MessageLogging(EventId = 121670, Level = LogLevel.Warning, Message = "No options table is named: the setting '{setting}' is not set")]
    public static partial IGenericMessage OptionsTableNotNamed(ILogger logger, string setting);
}
