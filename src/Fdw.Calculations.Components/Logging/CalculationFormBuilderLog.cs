#pragma warning disable CS1591
using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Calculations.Components.Logging;

/// <summary>
/// MessageLogging methods for CalculationFormBuilder operations.
/// EventId range: 9014-9025
/// </summary>
[MessageLoggingTypeCode("COMPONENTS2")]
public static partial class CalculationFormBuilderLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Validate Formula (9014-9016)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when formula validation starts.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "CalculationFormBuilder: Validating formula for DataSet '{dataSetName}'")]
    public static partial IGenericMessage ValidateFormulaStarted(
        ILogger logger,
        string dataSetName);

    /// <summary>Logs when formula validation succeeds.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Information,
        Message = "CalculationFormBuilder: Formula validation succeeded for DataSet '{dataSetName}', isValid={isValid}")]
    public static partial IGenericMessage ValidateFormulaSucceeded(
        ILogger logger,
        string dataSetName,
        bool isValid);

    /// <summary>Logs when formula validation fails (no exception).</summary>
    [MessageLogging(EventId = 21000, Level = LogLevel.Warning,
        Message = "CalculationFormBuilder: Formula validation returned a failure response for DataSet '{dataSetName}'")]
    public static partial IGenericMessage ValidateFormulaFailed(
        ILogger logger,
        string dataSetName);

    /// <summary>Logs when formula validation fails with an exception.</summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error,
        Message = "CalculationFormBuilder: Formula validation threw an exception for DataSet '{dataSetName}'")]
    public static partial IGenericMessage ValidateFormulaException(
        ILogger logger,
        Exception exception,
        string dataSetName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Get DataSet Fields (9018-9021)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading DataSet fields starts.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "CalculationFormBuilder: Loading fields for DataSet '{dataSetName}'")]
    public static partial IGenericMessage GetDataSetFieldsStarted(
        ILogger logger,
        string dataSetName);

    /// <summary>Logs when loading DataSet fields succeeds.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Information,
        Message = "CalculationFormBuilder: Loaded {fieldCount} field(s) for DataSet '{dataSetName}'")]
    public static partial IGenericMessage GetDataSetFieldsSucceeded(
        ILogger logger,
        int fieldCount,
        string dataSetName);

    /// <summary>Logs when loading DataSet fields fails (no exception).</summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Warning,
        Message = "CalculationFormBuilder: Failed to load fields for DataSet '{dataSetName}'")]
    public static partial IGenericMessage GetDataSetFieldsFailed(
        ILogger logger,
        string dataSetName);

    /// <summary>Logs when loading DataSet fields fails with an exception.</summary>
    [MessageLogging(EventId = 91001, Level = LogLevel.Error,
        Message = "CalculationFormBuilder: Exception loading fields for DataSet '{dataSetName}'")]
    public static partial IGenericMessage GetDataSetFieldsException(
        ILogger logger,
        Exception exception,
        string dataSetName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Language Changed (9022)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when the formula language selection changes.</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Information,
        Message = "CalculationFormBuilder: Formula language changed to '{language}'")]
    public static partial IGenericMessage LanguageChanged(
        ILogger logger,
        string language);
}
