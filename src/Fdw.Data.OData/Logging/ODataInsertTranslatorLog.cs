using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.OData.Logging;

/// <summary>
/// Message logging for <see cref="Fdw.Data.OData.ODataInsertTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("REST")]
public static partial class ODataInsertTranslatorLog
{
    [MessageLogging(
        EventId = 12020,
        Level = LogLevel.Trace,
        Message = "ODataInsertTranslator translating InsertCommand for container '{container}'")]
    public static partial IGenericMessage Translating(
        ILogger logger,
        string container);

    [MessageLogging(
        EventId = 12021,
        Level = LogLevel.Information,
        Message = "ODataInsertTranslator built POST request for container '{container}'")]
    public static partial IGenericMessage Translated(
        ILogger logger,
        string container);

    // Why: reuses ODataResultCodes.ContainerNull's number (20000).
    [MessageLogging(
        EventId = 20000,
        Level = LogLevel.Error,
        Message = "ODataInsertTranslator received a null container")]
    public static partial IGenericMessage ContainerNull(ILogger logger);

    // Why: reuses ODataResultCodes.InsertDataRequired's number (21001).
    [MessageLogging(
        EventId = 21001,
        Level = LogLevel.Error,
        Message = "ODataInsertTranslator: container '{container}' InsertCommand has no Data in metadata")]
    public static partial IGenericMessage InsertDataRequired(
        ILogger logger,
        string container);

    // Why: a fresh number, not ODataResultCodes.InsertTranslationFailed's own 91001 — see
    // ODataQueryTranslatorLog.QueryTranslationFailed's remark: 91001 already belongs to the
    // pre-existing (orphaned) ODataLog in this same REST TypeCode pool.
    [MessageLogging(
        EventId = 92001,
        Level = LogLevel.Error,
        Message = "ODataInsertTranslator failed to translate insert for container '{container}': {errorMessage}")]
    public static partial IGenericMessage InsertTranslationFailed(
        ILogger logger,
        System.Exception exception,
        string container,
        string errorMessage);
}
