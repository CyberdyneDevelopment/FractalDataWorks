using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Themes.Endpoints;

/// <summary>The coded messages the uithemes endpoints answer a refusal or a fault with: one per reason, each with the client text it shows (FDW-2335).</summary>
[MessageLoggingTypeCode("UITHEMESANSWERS")]
public static partial class UIThemesAnswerLog
{

    /// <summary>Logs that Theme was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30001, Level = LogLevel.Warning, Message = "Theme was not found, or is not visible to the caller", ClientMessage = "Theme was not found")]
    public static partial IGenericMessage UpdateThemeNotFound(ILogger logger);
}
