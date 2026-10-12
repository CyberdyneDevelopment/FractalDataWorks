using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.OData.Logging;

/// <summary>
/// Message logging for OData command translators.
/// EventId range: 4300-4349
/// </summary>
[MessageLoggingTypeCode("REST")]
public static partial class ODataLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Trace (4300-4309) - Detailed translation steps
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when starting OData query translation.
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Trace,
        Message = "Starting OData query translation for '{dataSetName}'")]
    public static partial IGenericMessage TranslationStarted(
        ILogger logger,
        string dataSetName);

    /// <summary>
    /// Logs the generated $filter parameter.
    /// </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Trace,
        Message = "Generated $filter: {filterExpression}")]
    public static partial IGenericMessage FilterGenerated(
        ILogger logger,
        string filterExpression);

    /// <summary>
    /// Logs the generated $select parameter.
    /// </summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Trace,
        Message = "Generated $select: {selectExpression}")]
    public static partial IGenericMessage SelectGenerated(
        ILogger logger,
        string selectExpression);

    /// <summary>
    /// Logs the generated $orderby parameter.
    /// </summary>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Trace,
        Message = "Generated $orderby: {orderByExpression}")]
    public static partial IGenericMessage OrderByGenerated(
        ILogger logger,
        string orderByExpression);

    /// <summary>
    /// Logs the generated paging parameters ($top, $skip).
    /// </summary>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Trace,
        Message = "Generated paging: $top={top}, $skip={skip}")]
    public static partial IGenericMessage PagingGenerated(
        ILogger logger,
        int top,
        int skip);

    // ═══════════════════════════════════════════════════════════════════════════
    // Debug (4310-4319) - Translation decisions and intermediate results
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs the complete OData URL being generated.
    /// </summary>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Debug,
        Message = "Generated OData URL: {url}")]
    public static partial IGenericMessage UrlGenerated(
        ILogger logger,
        string url);

    /// <summary>
    /// Logs when a filter operator is translated to OData syntax.
    /// </summary>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Debug,
        Message = "Translated filter operator '{operatorName}' to OData: {odataOperator}")]
    public static partial IGenericMessage OperatorTranslated(
        ILogger logger,
        string operatorName,
        string odataOperator);

    /// <summary>
    /// Logs when a field mapping is applied during translation.
    /// </summary>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Debug,
        Message = "Mapped field '{sourceField}' to OData property '{odataProperty}'")]
    public static partial IGenericMessage FieldMapped(
        ILogger logger,
        string sourceField,
        string odataProperty);

    // ═══════════════════════════════════════════════════════════════════════════
    // Information (4320-4329) - Key translation events
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs successful completion of OData translation.
    /// </summary>
    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Information,
        Message = "OData {commandType} translation completed for '{dataSetName}'")]
    public static partial IGenericMessage TranslationCompleted(
        ILogger logger,
        string commandType,
        string dataSetName);

    /// <summary>
    /// Logs when translation uses a fallback strategy.
    /// </summary>
    [MessageLogging(
        EventId = 11009,
        Level = LogLevel.Information,
        Message = "Using fallback translation for unsupported feature '{feature}' in '{dataSetName}'")]
    public static partial IGenericMessage FallbackUsed(
        ILogger logger,
        string feature,
        string dataSetName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Warning (4330-4339) - Translation issues that don't prevent completion
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when an unsupported filter operator is encountered.
    /// </summary>
    [MessageLogging(
        EventId = 61000,
        Level = LogLevel.Warning,
        Message = "Unsupported filter operator '{operatorName}' - skipping filter condition")]
    public static partial IGenericMessage UnsupportedOperator(
        ILogger logger,
        string operatorName);

    /// <summary>
    /// Logs when field projection is not supported and ignored.
    /// </summary>
    [MessageLogging(
        EventId = 61001,
        Level = LogLevel.Warning,
        Message = "Field '{fieldName}' projection not supported in OData - ignoring")]
    public static partial IGenericMessage UnsupportedProjection(
        ILogger logger,
        string fieldName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Error (4340-4349) - Translation failures
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when translation fails due to invalid input.
    /// </summary>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "OData translation failed for '{dataSetName}': {reason}")]
    public static partial IGenericMessage TranslationFailed(
        ILogger logger,
        string dataSetName,
        string reason);

    /// <summary>
    /// Logs when translation fails with an exception.
    /// </summary>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "OData translation exception for '{dataSetName}'")]
    public static partial IGenericMessage TranslationException(
        ILogger logger,
        Exception ex,
        string dataSetName);
}
