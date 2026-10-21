using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Logging;

/// <summary>
/// MessageLogging for settings that inherit and lock down the tenant and organization tree (FDW-1579).
/// EventId ranges: 12900-12901 (done), 22900-22901 (invalid input, 400), 32900-32901 (not found, 404), 42900 (conflict, 409),
/// 72900-72901 (store failures, 500).
/// </summary>
[MessageLoggingTypeCode("SETTINGS")]
public static partial class InheritedSettingsLog
{
    /// <summary>Logs that a setting was set at a level.</summary>
    [MessageLogging(EventId = 12900, Level = LogLevel.Information, Message = "Setting '{settingName}' was set at the {level} level (locked: {locked})")]
    public static partial IGenericMessage SettingSet(ILogger logger, string settingName, string level, bool locked);

    /// <summary>Logs that a lock was lifted.</summary>
    [MessageLogging(EventId = 12901, Level = LogLevel.Information, Message = "The lock on setting '{settingName}' held at the {level} level was lifted")]
    public static partial IGenericMessage LockLifted(ILogger logger, string settingName, string level);

    /// <summary>Logs that an organization cannot lock a setting.</summary>
    [MessageLogging(EventId = 22900, Level = LogLevel.Warning, Message = "Setting '{settingName}' cannot be locked at the Organization level: no level lies below it")]
    public static partial IGenericMessage OrganizationCannotLock(ILogger logger, string settingName);

    /// <summary>Logs that a scope does not name what its level needs.</summary>
    [MessageLogging(EventId = 22901, Level = LogLevel.Warning, Message = "A {level} scope was refused: it does not name its {what}")]
    public static partial IGenericMessage ScopeIncomplete(ILogger logger, string level, string what);

    /// <summary>Logs that the platform defines no such setting.</summary>
    [MessageLogging(EventId = 32900, Level = LogLevel.Warning, Message = "The platform defines no setting '{settingName}'")]
    public static partial IGenericMessage SettingNotFound(ILogger logger, string settingName);

    /// <summary>Logs that a level holds no lock on a setting.</summary>
    [MessageLogging(EventId = 32901, Level = LogLevel.Warning, Message = "Setting '{settingName}' holds no lock at the {level} level")]
    public static partial IGenericMessage NotLocked(ILogger logger, string settingName, string level);

    /// <summary>Logs that a setting is locked by a level above the one asked to set it.</summary>
    [MessageLogging(EventId = 42900, Level = LogLevel.Warning, Message = "Setting '{settingName}' was refused at the {level} level: it is locked at the {lockedAt} level")]
    public static partial IGenericMessage SettingLocked(ILogger logger, string settingName, string level, string lockedAt);

    /// <summary>Logs that a read from the store failed.</summary>
    [MessageLogging(EventId = 72900, Level = LogLevel.Error, Message = "{what} could not be read: {reason}")]
    public static partial IGenericMessage ReadFailed(ILogger logger, string what, string? reason);

    /// <summary>Logs that a write to the store failed.</summary>
    [MessageLogging(EventId = 72901, Level = LogLevel.Error, Message = "{what} could not be written: {reason}")]
    public static partial IGenericMessage WriteFailed(ILogger logger, string what, string? reason);
}
