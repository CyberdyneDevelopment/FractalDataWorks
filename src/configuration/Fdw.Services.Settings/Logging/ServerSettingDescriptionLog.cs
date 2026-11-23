using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Logging;

/// <summary>Messages of the write of a server setting's description (FDW-7256).</summary>
[MessageLoggingTypeCode("SETTINGDESCWRITE")]
public static partial class ServerSettingDescriptionLog
{
    /// <summary>Logs that a setting was saved with a description in a unit the caller opened, which the save could not prepare: the language is resolved before a unit opens, never inside it.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="settingName">The setting that was saved.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 60385, Level = LogLevel.Error,
        Message = "The server setting '{settingName}' was saved with a description into a unit the caller opened: the description language is resolved before a unit opens, never inside it, because it is read from the tables the unit has locked; save the setting without a unit of your own, or without a description",
        ClientMessage = "A server setting's description can be saved only by a save that opens its own unit")]
    public static partial IGenericMessage NotPrepared(ILogger logger, string settingName);
}
