using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.OData.Logging;

/// <summary>
/// Message logging for <see cref="Fdw.Data.OData.ODataDeleteTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("REST")]
public static partial class ODataDeleteTranslatorLog
{
    [MessageLogging(
        EventId = 12040,
        Level = LogLevel.Trace,
        Message = "ODataDeleteTranslator translating DeleteCommand for container '{container}'")]
    public static partial IGenericMessage Translating(
        ILogger logger,
        string container);

    [MessageLogging(
        EventId = 12041,
        Level = LogLevel.Information,
        Message = "ODataDeleteTranslator built DELETE request for container '{container}'")]
    public static partial IGenericMessage Translated(
        ILogger logger,
        string container);

    // Why: reuses ODataResultCodes.ContainerNull's number (20000).
    [MessageLogging(
        EventId = 20000,
        Level = LogLevel.Error,
        Message = "ODataDeleteTranslator received a null container")]
    public static partial IGenericMessage ContainerNull(ILogger logger);

    // Why: reuses ODataResultCodes.DeleteFilterRequired's number (21000).
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Error,
        Message = "ODataDeleteTranslator: container '{container}' DeleteCommand has no Filter in metadata")]
    public static partial IGenericMessage DeleteFilterRequired(
        ILogger logger,
        string container);

    // Why: reuses ODataResultCodes.DeleteFilterInvalid's number (20001).
    [MessageLogging(
        EventId = 20001,
        Level = LogLevel.Error,
        Message = "ODataDeleteTranslator: container '{container}' DeleteCommand Filter has no Root node")]
    public static partial IGenericMessage DeleteFilterInvalid(
        ILogger logger,
        string container);

    // Why: reuses ODataResultCodes.DeleteResourceIdNotFound's number (21003).
    [MessageLogging(
        EventId = 21003,
        Level = LogLevel.Error,
        Message = "ODataDeleteTranslator: cannot determine resource id from Filter for container '{container}'")]
    public static partial IGenericMessage DeleteResourceIdNotFound(
        ILogger logger,
        string container);

    // Why: a fresh number, not ODataResultCodes.DeleteTranslationFailed's own 91000 — see
    // ODataQueryTranslatorLog.QueryTranslationFailed's remark: 91000 already belongs to the
    // pre-existing (orphaned) ODataLog in this same REST TypeCode pool.
    [MessageLogging(
        EventId = 92003,
        Level = LogLevel.Error,
        Message = "ODataDeleteTranslator failed to translate delete for container '{container}': {errorMessage}")]
    public static partial IGenericMessage DeleteTranslationFailed(
        ILogger logger,
        System.Exception exception,
        string container,
        string errorMessage);
}
