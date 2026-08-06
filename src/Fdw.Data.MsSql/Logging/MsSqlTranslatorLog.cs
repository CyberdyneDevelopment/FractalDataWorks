using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.MsSql.Logging;

/// <summary>
/// Message logging for MsSql data command translators.
/// </summary>
[MessageLoggingTypeCode("MSSQL")]
public static partial class MsSqlTranslatorLog
{
    /// <summary>
    /// Logs when a translator receives an invalid command type.
    /// </summary>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Translator '{translatorName}' expected {expectedType} but received {actualType}")]
    public static partial IGenericMessage InvalidCommandType(
        ILogger logger,
        string translatorName,
        string expectedType,
        string actualType);

    /// <summary>
    /// Logs when a container has no declared fields and no projection — the translator
    /// refuses to emit <c>SELECT *</c>.
    /// </summary>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Error,
        Message = "Cannot translate query for container '{containerName}': no declared fields and no projection. SELECT * is not permitted.")]
    public static partial IGenericMessage NoFieldsToProject(
        ILogger logger,
        string containerName);

    /// <summary>
    /// Logs when the container passed to an MsSql translator does not implement IDataContainer.
    /// MsSql translators require structured key/field metadata only available on IDataContainer.
    /// </summary>
    [MessageLogging(
        EventId = 21001,
        Level = LogLevel.Error,
        Message = "MsSql translator cannot operate on container '{containerName}': container does not implement IDataContainer. Only structured data containers (MsSqlDataContainer) are valid for MsSql translators.")]
    public static partial IGenericMessage ContainerNotDataContainer(
        ILogger logger,
        string containerName);
}
