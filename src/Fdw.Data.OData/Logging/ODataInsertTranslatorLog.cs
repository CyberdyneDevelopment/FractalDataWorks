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

    [MessageLogging(
        EventId = 20000,
        Level = LogLevel.Error,
        Message = "ODataInsertTranslator received a null container")]
    public static partial IGenericMessage ContainerNull(ILogger logger);

    [MessageLogging(
        EventId = 21001,
        Level = LogLevel.Error,
        Message = "ODataInsertTranslator: container '{container}' InsertCommand has no Data in metadata")]
    public static partial IGenericMessage InsertDataRequired(
        ILogger logger,
        string container);

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
