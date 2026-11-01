using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Calculations.Logging;

/// <summary>
/// MessageLogging for calculation entity operations.
/// EventId ranges: 4140-4179 (core ops), 4058-4060 (typed config save — free block within 4058-4099)
/// </summary>
[MessageLoggingTypeCode("CALCULATIONS")]
internal static partial class CalculationEntityLog
{
    [MessageLogging(EventId = 11024, Level = LogLevel.Information, Message = "Getting calculation '{name}'")]
    public static partial IGenericMessage GetCalculationStarted(ILogger logger, string name);

    [MessageLogging(EventId = 11025, Level = LogLevel.Information, Message = "Got calculation '{name}'")]
    public static partial IGenericMessage GetCalculationSucceeded(ILogger logger, string name);

    [MessageLogging(EventId = 71008, Level = LogLevel.Error, Message = "Failed to get calculation '{name}'")]
    public static partial IGenericMessage GetCalculationFailed(ILogger logger, Exception ex, string name);

    [MessageLogging(EventId = 11026, Level = LogLevel.Information, Message = "Listing calculations")]
    public static partial IGenericMessage ListCalculationsStarted(ILogger logger);

    [MessageLogging(EventId = 11027, Level = LogLevel.Information, Message = "Listed {count} calculations")]
    public static partial IGenericMessage ListCalculationsSucceeded(ILogger logger, int count);

    [MessageLogging(EventId = 71009, Level = LogLevel.Error, Message = "Failed to list calculations")]
    public static partial IGenericMessage ListCalculationsFailed(ILogger logger, Exception ex);

    [MessageLogging(EventId = 11028, Level = LogLevel.Information, Message = "Executing calculation '{name}'")]
    public static partial IGenericMessage ExecuteCalculationStarted(ILogger logger, string name);

    [MessageLogging(EventId = 11029, Level = LogLevel.Information, Message = "Calculation '{name}' completed")]
    public static partial IGenericMessage ExecuteCalculationSucceeded(ILogger logger, string name);

    [MessageLogging(EventId = 91001, Level = LogLevel.Error, Message = "Calculation '{name}' failed")]
    public static partial IGenericMessage ExecuteCalculationFailed(ILogger logger, Exception ex, string name);

    [MessageLogging(EventId = 11030, Level = LogLevel.Information, Message = "Validating calculation '{name}'")]
    public static partial IGenericMessage ValidateCalculationStarted(ILogger logger, string name);

    [MessageLogging(EventId = 21001, Level = LogLevel.Error, Message = "Calculation '{name}' validation failed")]
    public static partial IGenericMessage ValidateCalculationFailed(ILogger logger, Exception ex, string name);

    [MessageLogging(EventId = 11031, Level = LogLevel.Information, Message = "Calculation '{name}' passed validation")]
    public static partial IGenericMessage ValidateCalculationPassed(ILogger logger, string name);

    [MessageLogging(EventId = 71010, Level = LogLevel.Error, Message = "Failed to load calculation '{name}'")]
    public static partial IGenericMessage CalculationLoadFailed(ILogger logger, Exception ex, string name);

    [MessageLogging(EventId = 91002, Level = LogLevel.Error, Message = "Calculation '{name}' execute failed")]
    public static partial IGenericMessage CalculationExecuteFailed(ILogger logger, Exception ex, string name);

    [MessageLogging(EventId = 11032, Level = LogLevel.Information, Message = "Executing calculation '{name}'")]
    public static partial IGenericMessage CalculationExecuteStarted(ILogger logger, string name);

    [MessageLogging(EventId = 21002, Level = LogLevel.Error, Message = "Calculation '{name}' validation failed: {message}")]
    public static partial IGenericMessage CalculationValidationFailed(ILogger logger, string name, string message);

    [MessageLogging(EventId = 91003, Level = LogLevel.Warning, Message = "Calculation service not yet implemented for '{name}'")]
    public static partial IGenericMessage CalculationServiceNotImplemented(ILogger logger, string name);

    [MessageLogging(EventId = 31000, Level = LogLevel.Warning, Message = "Calculation '{name}' not found in registry")]
    public static partial IGenericMessage CalculationNotFound(ILogger logger, string name);

    // --- Input resolution ---

    [MessageLogging(EventId = 11033, Level = LogLevel.Debug, Message = "Resolving input '{alias}' of kind '{kind}'")]
    public static partial IGenericMessage InputResolutionStarted(ILogger logger, string alias, string kind);

    [MessageLogging(EventId = 11034, Level = LogLevel.Debug, Message = "Resolved input '{alias}' with {rowCount} items")]
    public static partial IGenericMessage InputResolutionSucceeded(ILogger logger, string alias, int rowCount);

    [MessageLogging(EventId = 31001, Level = LogLevel.Error, Message = "Failed to resolve input '{alias}'")]
    public static partial IGenericMessage InputResolutionFailed(ILogger logger, Exception ex, string alias);

    [MessageLogging(EventId = 11035, Level = LogLevel.Warning, Message = "Skipped input '{alias}': {reason}")]
    public static partial IGenericMessage InputResolutionSkipped(ILogger logger, string alias, string reason);

    [MessageLogging(EventId = 21022, Level = LogLevel.Warning,
        Message = "Calculation input '{alias}' is a {kind} input, which the calculation service does not read yet",
        ClientMessage = "Calculation input '{alias}' is a {kind} input, which calculations do not read yet")]
    public static partial IGenericMessage InputKindNotRead(ILogger logger, string alias, string kind);

    [MessageLogging(EventId = 21026, Level = LogLevel.Warning,
        Message = "Calculation input '{alias}' is a DataSet input with no data set name",
        ClientMessage = "Calculation input '{alias}' is a DataSet input and names no data set")]
    public static partial IGenericMessage DataSetInputNamesNoDataSet(ILogger logger, string alias);

    [MessageLogging(EventId = 21027, Level = LogLevel.Warning,
        Message = "Calculation input '{alias}' is a DataSet input, and this run states no row cap, so it has no caller to read the data set as",
        ClientMessage = "Calculation input '{alias}' is a DataSet input, and a calculation reads a data set as the caller who runs it; this run has no caller")]
    public static partial IGenericMessage DataSetInputHasNoCaller(ILogger logger, string alias);

    [MessageLogging(EventId = 61021, Level = LogLevel.Error,
        Message = "Calculation input '{alias}' is a DataSet input, and no IDataSetRowReader is registered to read it")]
    public static partial IGenericMessage DataSetRowReaderNotRegistered(ILogger logger, string alias);

    [MessageLogging(EventId = 21023, Level = LogLevel.Warning,
        Message = "Calculation '{name}' is a {entityType} calculation with no input rows to compute over",
        ClientMessage = "Calculation '{name}' has no input rows to compute over; give it a DataSet input that provides rows")]
    public static partial IGenericMessage NoInputRows(ILogger logger, string name, string entityType);

    // --- Windowed execution ---

    [MessageLogging(EventId = 11036, Level = LogLevel.Information, Message = "Starting windowed execution '{name}' with function '{windowFunction}' across {partitionCount} partitions")]
    public static partial IGenericMessage WindowedExecutionStarted(ILogger logger, string name, string windowFunction, int partitionCount);

    [MessageLogging(EventId = 11037, Level = LogLevel.Information, Message = "Windowed execution '{name}' completed with {rowCount} rows")]
    public static partial IGenericMessage WindowedExecutionSucceeded(ILogger logger, string name, int rowCount);

    [MessageLogging(EventId = 91004, Level = LogLevel.Error, Message = "Windowed execution '{name}' failed")]
    public static partial IGenericMessage WindowedExecutionFailed(ILogger logger, Exception ex, string name);

    [MessageLogging(EventId = 21020, Level = LogLevel.Warning,
        Message = "Calculation '{name}' names window function '{windowFunction}', which is not offered; the offered functions are {offered}",
        ClientMessage = "The window function '{windowFunction}' is not offered; the offered functions are {offered}")]
    public static partial IGenericMessage WindowFunctionNotOffered(ILogger logger, string name, string windowFunction, string offered);

    [MessageLogging(EventId = 21021, Level = LogLevel.Warning,
        Message = "Calculation '{name}' was given a windowed spec that is a {suppliedType}, not a WindowedCalculationSpec",
        ClientMessage = "The windowed spec is not a WindowedCalculationSpec")]
    public static partial IGenericMessage WindowedSpecInvalid(ILogger logger, string name, string suppliedType);

    [MessageLogging(EventId = 61020, Level = LogLevel.Error, Message = "Calculation '{name}' has no windowed configuration loaded")]
    public static partial IGenericMessage WindowedConfigurationNotLoaded(ILogger logger, string name);

    // --- CRUD operations ---

    [MessageLogging(EventId = 11042, Level = LogLevel.Information, Message = "Creating calculation '{name}'")]
    public static partial IGenericMessage CreateCalculationStarted(ILogger logger, string name);

    [MessageLogging(EventId = 11043, Level = LogLevel.Information, Message = "Created calculation '{name}' with ID {id}")]
    public static partial IGenericMessage CreateCalculationSucceeded(ILogger logger, string name, Guid id);

    [MessageLogging(EventId = 71011, Level = LogLevel.Error, Message = "Failed to create calculation '{name}'")]
    public static partial IGenericMessage CreateCalculationFailed(ILogger logger, Exception ex, string name);

    [MessageLogging(EventId = 11044, Level = LogLevel.Information, Message = "Updating calculation '{id}'")]
    public static partial IGenericMessage UpdateCalculationStarted(ILogger logger, Guid id);

    [MessageLogging(EventId = 11045, Level = LogLevel.Information, Message = "Updated calculation '{id}'")]
    public static partial IGenericMessage UpdateCalculationSucceeded(ILogger logger, Guid id);

    [MessageLogging(EventId = 71012, Level = LogLevel.Error, Message = "Failed to update calculation '{id}'")]
    public static partial IGenericMessage UpdateCalculationFailed(ILogger logger, Exception ex, Guid id);

    [MessageLogging(EventId = 11046, Level = LogLevel.Information, Message = "Deleting calculation '{id}'")]
    public static partial IGenericMessage DeleteCalculationStarted(ILogger logger, Guid id);

    [MessageLogging(EventId = 11047, Level = LogLevel.Information, Message = "Deleted calculation '{id}'")]
    public static partial IGenericMessage DeleteCalculationSucceeded(ILogger logger, Guid id);

    [MessageLogging(EventId = 71013, Level = LogLevel.Error, Message = "Failed to delete calculation '{id}'")]
    public static partial IGenericMessage DeleteCalculationFailed(ILogger logger, Exception ex, Guid id);

    // --- Typed configuration save ---

    [MessageLogging(EventId = 11048, Level = LogLevel.Information, Message = "Saved typed configuration for calculation entity '{id}'")]
    public static partial IGenericMessage TypedConfigurationSaved(ILogger logger, Guid id);

    [MessageLogging(EventId = 71014, Level = LogLevel.Error, Message = "Failed to save typed configuration for calculation entity '{id}': {reason}")]
    public static partial IGenericMessage TypedConfigurationSaveFailed(ILogger logger, Guid id, string reason);

    [MessageLogging(EventId = 41000, Level = LogLevel.Error, Message = "Unknown calculation entity type '{typeName}' — cannot save typed configuration")]
    public static partial IGenericMessage TypedConfigurationSaveUnknownType(ILogger logger, string typeName);

    [MessageLogging(EventId = 61001, Level = LogLevel.Error, Message = "Entity type '{typeName}' declares no typed container — cannot persist provided typed configuration")]
    public static partial IGenericMessage TypedConfigurationNoContainer(ILogger logger, string typeName);
}
