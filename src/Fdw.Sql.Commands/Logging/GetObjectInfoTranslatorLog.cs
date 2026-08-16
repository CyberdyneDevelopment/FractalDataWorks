using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Sql.Commands.Logging;

/// <summary>Message logging for <see cref="Fdw.Sql.Commands.Project.Translators.GetObjectInfoTranslator"/>.</summary>
[MessageLoggingTypeCode("SQL")]
public static partial class GetObjectInfoTranslatorLog
{
    /// <summary>Logs translator entry.</summary>
    [MessageLogging(
        EventId = 11027,
        Level = LogLevel.Trace,
        Message = "GetObjectInfoTranslator translating GetObjectInfoCommand for '{objectName}' (schema '{schema}')")]
    public static partial IGenericMessage Translating(
        ILogger logger,
        string objectName,
        string schema);

    /// <summary>Logs that the command's ObjectName was missing.</summary>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Error,
        Message = "GetObjectInfoTranslator: ObjectName is required")]
    public static partial IGenericMessage ObjectNameRequired(
        ILogger logger);

    /// <summary>Logs that no matching object was found in the workspace model.</summary>
    [MessageLogging(
        EventId = 31000,
        Level = LogLevel.Error,
        Message = "GetObjectInfoTranslator: object '{objectName}' not found")]
    public static partial IGenericMessage ObjectNotFound(
        ILogger logger,
        string objectName);

    /// <summary>Logs that DacFx could not script the matched object; continuing with a null Definition.</summary>
    [MessageLogging(
        EventId = 81000,
        Level = LogLevel.Warning,
        Message = "GetObjectInfoTranslator could not script object '{fullName}' ({kind}); continuing with null Definition")]
    public static partial IGenericMessage ScriptUnavailable(
        ILogger logger,
        string fullName,
        string kind);

    /// <summary>Logs a successful object lookup.</summary>
    [MessageLogging(
        EventId = 13000,
        Level = LogLevel.Information,
        Message = "GetObjectInfoTranslator found object '{fullName}' ({kind})")]
    public static partial IGenericMessage ObjectFound(
        ILogger logger,
        string fullName,
        string kind);
}
