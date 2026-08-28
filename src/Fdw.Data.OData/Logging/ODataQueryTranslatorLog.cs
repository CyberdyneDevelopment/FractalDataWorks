using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.OData.Logging;

/// <summary>
/// Message logging for <see cref="Fdw.Data.OData.ODataQueryTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("REST")]
public static partial class ODataQueryTranslatorLog
{
    [MessageLogging(
        EventId = 12010,
        Level = LogLevel.Trace,
        Message = "ODataQueryTranslator translating QueryCommand for container '{container}'")]
    public static partial IGenericMessage Translating(
        ILogger logger,
        string container);

    [MessageLogging(
        EventId = 12011,
        Level = LogLevel.Information,
        Message = "ODataQueryTranslator built GET request '{url}' for container '{container}'")]
    public static partial IGenericMessage Translated(
        ILogger logger,
        string container,
        string url);

    [MessageLogging(
        EventId = 20000,
        Level = LogLevel.Error,
        Message = "ODataQueryTranslator received a null container")]
    public static partial IGenericMessage ContainerNull(ILogger logger);

    [MessageLogging(
        EventId = 92000,
        Level = LogLevel.Error,
        Message = "ODataQueryTranslator failed to translate query for container '{container}': {errorMessage}")]
    public static partial IGenericMessage QueryTranslationFailed(
        ILogger logger,
        System.Exception exception,
        string container,
        string errorMessage);
}
