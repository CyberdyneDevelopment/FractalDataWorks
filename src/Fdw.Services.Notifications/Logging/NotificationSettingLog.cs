using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Notifications.Logging;

/// <summary>
/// Structured log messages for the refusal of the settings of a notification (FDW-2931). Each is a validation refusal (category 2)
/// whose client message names the setting.
/// </summary>
[MessageLoggingTypeCode("NOTIFICATION")]
public static partial class NotificationSettingLog
{
    /// <summary>Refuses a setting the notification type does not have.</summary>
    [MessageLogging(
        EventId = 21030,
        Level = LogLevel.Warning,
        Message = "Setting '{setting}' is not a setting of the {type} notification type",
        ClientMessage = "Setting '{setting}' is not a setting of the {type} notification type")]
    public static partial IGenericMessage SettingUnknown(ILogger logger, string setting, string type);

    /// <summary>Refuses a value that does not read as the type the setting declares.</summary>
    [MessageLogging(
        EventId = 21031,
        Level = LogLevel.Warning,
        Message = "Setting '{setting}' was refused: its value is not a {dataType}",
        ClientMessage = "Setting '{setting}' must be a {dataType}")]
    public static partial IGenericMessage SettingNotReadable(ILogger logger, string setting, string dataType);

    /// <summary>Refuses a value given for a secret setting.</summary>
    [MessageLogging(
        EventId = 21032,
        Level = LogLevel.Warning,
        Message = "Setting '{setting}' was refused: it is a secret and its value was sent",
        ClientMessage = "Setting '{setting}' is a secret: do not send its value, name it with secretManagerName and secretKeyName")]
    public static partial IGenericMessage SettingIsSecret(ILogger logger, string setting);

    /// <summary>Refuses a notification made without a setting the type requires.</summary>
    [MessageLogging(
        EventId = 21033,
        Level = LogLevel.Warning,
        Message = "A {type} notification was refused: setting '{setting}' is required",
        ClientMessage = "Setting '{setting}' is required for a {type} notification")]
    public static partial IGenericMessage SettingMissing(ILogger logger, string setting, string type);

    /// <summary>Refuses a notification that needs a secret it does not name.</summary>
    [MessageLogging(
        EventId = 21034,
        Level = LogLevel.Warning,
        Message = "A {type} notification was refused: secret setting '{setting}' is not named",
        ClientMessage = "Setting '{setting}' is a secret and is not named: give secretManagerName and secretKeyName")]
    public static partial IGenericMessage SettingSecretNotNamed(ILogger logger, string setting, string type);
}
