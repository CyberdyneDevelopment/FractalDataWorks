using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>The coded messages the settings endpoints answer a refusal or a fault with: one per reason, each with the client text it shows (FDW-2335).</summary>
[MessageLoggingTypeCode("SETTINGSANSWERS")]
public static partial class SettingsAnswerLog
{

    /// <summary>Logs that The request to publish person home page was refused: the caller is not signed in.</summary>
    [MessageLogging(EventId = 50001, Level = LogLevel.Warning, Message = "The request to publish person home page was refused: the caller is not signed in", ClientMessage = "You must be signed in to publish person home page")]
    public static partial IGenericMessage PublishPersonHomePageNotSignedIn(ILogger logger);

    /// <summary>Logs that The request to save person home page draft was refused: the caller is not signed in.</summary>
    [MessageLogging(EventId = 50002, Level = LogLevel.Warning, Message = "The request to save person home page draft was refused: the caller is not signed in", ClientMessage = "You must be signed in to save person home page draft")]
    public static partial IGenericMessage SavePersonHomePageDraftNotSignedIn(ILogger logger);

    /// <summary>Logs that The request to get home page was refused: the caller is not signed in.</summary>
    [MessageLogging(EventId = 50003, Level = LogLevel.Warning, Message = "The request to get home page was refused: the caller is not signed in", ClientMessage = "You must be signed in to get home page")]
    public static partial IGenericMessage GetHomePageNotSignedIn(ILogger logger);

    /// <summary>Logs that The request to get person home page was refused: the caller is not signed in.</summary>
    [MessageLogging(EventId = 50004, Level = LogLevel.Warning, Message = "The request to get person home page was refused: the caller is not signed in", ClientMessage = "You must be signed in to get person home page")]
    public static partial IGenericMessage GetPersonHomePageNotSignedIn(ILogger logger);
}
