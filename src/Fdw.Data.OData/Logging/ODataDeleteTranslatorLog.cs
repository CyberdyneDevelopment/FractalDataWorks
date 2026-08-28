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

    [MessageLogging(
        EventId = 20000,
        Level = LogLevel.Error,
        Message = "ODataDeleteTranslator received a null container")]
    public static partial IGenericMessage ContainerNull(ILogger logger);

    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Error,
        Message = "ODataDeleteTranslator: container '{container}' DeleteCommand has no Filter in metadata")]
    public static partial IGenericMessage DeleteFilterRequired(
        ILogger logger,
        string container);

    [MessageLogging(
        EventId = 20001,
        Level = LogLevel.Error,
        Message = "ODataDeleteTranslator: container '{container}' DeleteCommand Filter has no Root node")]
    public static partial IGenericMessage DeleteFilterInvalid(
        ILogger logger,
        string container);

    [MessageLogging(
        EventId = 21003,
        Level = LogLevel.Error,
        Message = "ODataDeleteTranslator: cannot determine resource id from Filter for container '{container}'")]
    public static partial IGenericMessage DeleteResourceIdNotFound(
        ILogger logger,
        string container);

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
