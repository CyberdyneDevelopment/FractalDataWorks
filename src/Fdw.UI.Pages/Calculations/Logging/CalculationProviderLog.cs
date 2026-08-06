using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Calculations.Components.Logging;

/// <summary>
/// MessageLogging methods for CalculationProvider operations.
/// Provider-specific messages with domain context baked into templates.
/// EventId range: 8994-9013
/// </summary>
[MessageLoggingTypeCode("COMPONENTS2")]
public static partial class CalculationProviderLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Load Calculations (8994-8995)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading the calculations list fails.</summary>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error,
        Message = "CalculationProvider: Failed to load calculations list")]
    public static partial IGenericMessage LoadCalculationsFailed(
        ILogger logger);

    /// <summary>Logs when loading the calculations list fails with exception.</summary>
    [MessageLogging(EventId = 91002, Level = LogLevel.Error,
        Message = "CalculationProvider: Failed to load calculations list")]
    public static partial IGenericMessage LoadCalculationsException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Load Calculation Detail (8996-8997)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading calculation details fails.</summary>
    [MessageLogging(EventId = 71002, Level = LogLevel.Error,
        Message = "CalculationProvider: Failed to load calculation detail for '{calculationName}'")]
    public static partial IGenericMessage LoadCalculationDetailFailed(
        ILogger logger,
        string calculationName);

    /// <summary>Logs when loading calculation details fails with exception.</summary>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "CalculationProvider: Failed to load calculation detail")]
    public static partial IGenericMessage LoadCalculationDetailException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Create Calculation (8998-8999)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when creating a calculation fails.</summary>
    [MessageLogging(EventId = 91004, Level = LogLevel.Error,
        Message = "CalculationProvider: Failed to create calculation")]
    public static partial IGenericMessage CreateCalculationFailed(
        ILogger logger);

    /// <summary>Logs when creating a calculation fails with exception.</summary>
    [MessageLogging(EventId = 91005, Level = LogLevel.Error,
        Message = "CalculationProvider: Failed to create calculation")]
    public static partial IGenericMessage CreateCalculationException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Update Calculation (9000-9001)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when updating a calculation fails.</summary>
    [MessageLogging(EventId = 91006, Level = LogLevel.Error,
        Message = "CalculationProvider: Failed to update calculation '{calculationName}'")]
    public static partial IGenericMessage UpdateCalculationFailed(
        ILogger logger,
        string calculationName);

    /// <summary>Logs when updating a calculation fails with exception.</summary>
    [MessageLogging(EventId = 91007, Level = LogLevel.Error,
        Message = "CalculationProvider: Failed to update calculation")]
    public static partial IGenericMessage UpdateCalculationException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Delete Calculation (9002-9003)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when deleting a calculation fails.</summary>
    [MessageLogging(EventId = 91008, Level = LogLevel.Error,
        Message = "CalculationProvider: Failed to delete calculation '{calculationName}'")]
    public static partial IGenericMessage DeleteCalculationFailed(
        ILogger logger,
        string calculationName);

    /// <summary>Logs when deleting a calculation fails with exception.</summary>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "CalculationProvider: Failed to delete calculation")]
    public static partial IGenericMessage DeleteCalculationException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Validate Formula (9004-9005)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when validating a formula fails.</summary>
    [MessageLogging(EventId = 91010, Level = LogLevel.Error,
        Message = "CalculationProvider: Failed to validate formula")]
    public static partial IGenericMessage ValidateFormulaFailed(
        ILogger logger);

    /// <summary>Logs when validating a formula fails with exception.</summary>
    [MessageLogging(EventId = 91011, Level = LogLevel.Error,
        Message = "CalculationProvider: Failed to validate formula")]
    public static partial IGenericMessage ValidateFormulaException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Get DataSet Fields (9006-9007)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when getting data set fields fails.</summary>
    [MessageLogging(EventId = 91012, Level = LogLevel.Error,
        Message = "CalculationProvider: Failed to get data set fields")]
    public static partial IGenericMessage GetDataSetFieldsFailed(
        ILogger logger);

    /// <summary>Logs when getting data set fields fails with exception.</summary>
    [MessageLogging(EventId = 91013, Level = LogLevel.Error,
        Message = "CalculationProvider: Failed to get data set fields")]
    public static partial IGenericMessage GetDataSetFieldsException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Get Calculation Types (9008-9009)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when getting calculation types fails.</summary>
    [MessageLogging(EventId = 91014, Level = LogLevel.Error,
        Message = "CalculationProvider: Failed to get calculation types")]
    public static partial IGenericMessage GetCalculationTypesFailed(
        ILogger logger);

    /// <summary>Logs when getting calculation types fails with exception.</summary>
    [MessageLogging(EventId = 91015, Level = LogLevel.Error,
        Message = "CalculationProvider: Failed to get calculation types")]
    public static partial IGenericMessage GetCalculationTypesException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Preview Calculation (9010-9011)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when previewing a calculation fails.</summary>
    [MessageLogging(EventId = 91016, Level = LogLevel.Error,
        Message = "CalculationProvider: Failed to preview calculation")]
    public static partial IGenericMessage PreviewCalculationFailed(
        ILogger logger);

    /// <summary>Logs when previewing a calculation fails with exception.</summary>
    [MessageLogging(EventId = 91017, Level = LogLevel.Error,
        Message = "CalculationProvider: Failed to preview calculation")]
    public static partial IGenericMessage PreviewCalculationException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Execute Calculation (9012-9013)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when executing a calculation fails.</summary>
    [MessageLogging(EventId = 91018, Level = LogLevel.Error,
        Message = "CalculationProvider: Failed to execute calculation")]
    public static partial IGenericMessage ExecuteCalculationFailed(
        ILogger logger);

    /// <summary>Logs when executing a calculation fails with exception.</summary>
    [MessageLogging(EventId = 91019, Level = LogLevel.Error,
        Message = "CalculationProvider: Failed to execute calculation")]
    public static partial IGenericMessage ExecuteCalculationException(
        ILogger logger,
        Exception exception);
}
