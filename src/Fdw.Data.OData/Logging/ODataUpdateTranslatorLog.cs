using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.OData.Logging;

/// <summary>
/// Message logging for <see cref="Fdw.Data.OData.ODataUpdateTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("REST")]
public static partial class ODataUpdateTranslatorLog
{
    [MessageLogging(
        EventId = 12030,
        Level = LogLevel.Trace,
        Message = "ODataUpdateTranslator translating UpdateCommand for container '{container}'")]
    public static partial IGenericMessage Translating(
        ILogger logger,
        string container);

    /// <summary>
    /// Logs the resolved resource id used to build the PUT path.
    /// </summary>
    [MessageLogging(
        EventId = 12031,
        Level = LogLevel.Debug,
        Message = "ODataUpdateTranslator resolved resource id '{resourceId}' for container '{container}'")]
    public static partial IGenericMessage ResourceIdResolved(
        ILogger logger,
        string container,
        string resourceId);

    [MessageLogging(
        EventId = 12032,
        Level = LogLevel.Information,
        Message = "ODataUpdateTranslator built PUT request for container '{container}'")]
    public static partial IGenericMessage Translated(
        ILogger logger,
        string container);

    [MessageLogging(
        EventId = 20000,
        Level = LogLevel.Error,
        Message = "ODataUpdateTranslator received a null container")]
    public static partial IGenericMessage ContainerNull(ILogger logger);

    [MessageLogging(
        EventId = 21002,
        Level = LogLevel.Error,
        Message = "ODataUpdateTranslator: container '{container}' UpdateCommand has no Data in metadata")]
    public static partial IGenericMessage UpdateDataRequired(
        ILogger logger,
        string container);

    [MessageLogging(
        EventId = 30000,
        Level = LogLevel.Error,
        Message = "ODataUpdateTranslator: cannot determine resource id for container '{container}' — need Filter or primary key in data")]
    public static partial IGenericMessage UpdateResourceIdNotFound(
        ILogger logger,
        string container);

    [MessageLogging(
        EventId = 92002,
        Level = LogLevel.Error,
        Message = "ODataUpdateTranslator failed to translate update for container '{container}': {errorMessage}")]
    public static partial IGenericMessage UpdateTranslationFailed(
        ILogger logger,
        System.Exception exception,
        string container,
        string errorMessage);
}
