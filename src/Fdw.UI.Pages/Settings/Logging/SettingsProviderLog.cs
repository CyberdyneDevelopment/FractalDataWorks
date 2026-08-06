using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Settings.Components.Logging;

/// <summary>
/// MessageLogging methods for the SettingsProvider headless component.
/// EventId range: 4220-4239
/// </summary>
[MessageLoggingTypeCode("COMPONENTS17")]
public static partial class SettingsProviderLog
{
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "SettingsProvider: Loading server settings")]
    public static partial IGenericMessage LoadSettingsStarted(ILogger logger);

    [MessageLogging(EventId = 11001, Level = LogLevel.Information,
        Message = "SettingsProvider: Loaded {count} server settings")]
    public static partial IGenericMessage LoadSettingsCompleted(ILogger logger, int count);

    [MessageLogging(EventId = 71000, Level = LogLevel.Warning,
        Message = "SettingsProvider: Failed to load server settings")]
    public static partial IGenericMessage LoadSettingsFailed(ILogger logger);

    [MessageLogging(EventId = 71001, Level = LogLevel.Warning,
        Message = "SettingsProvider: Failed to load server settings")]
    public static partial IGenericMessage LoadSettingsException(ILogger logger, Exception exception);

    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "SettingsProvider: Loading themes")]
    public static partial IGenericMessage LoadThemesStarted(ILogger logger);

    [MessageLogging(EventId = 11003, Level = LogLevel.Information,
        Message = "SettingsProvider: Loaded {count} themes")]
    public static partial IGenericMessage LoadThemesCompleted(ILogger logger, int count);

    [MessageLogging(EventId = 71002, Level = LogLevel.Warning,
        Message = "SettingsProvider: Failed to load themes")]
    public static partial IGenericMessage LoadThemesFailed(ILogger logger);

    [MessageLogging(EventId = 71003, Level = LogLevel.Warning,
        Message = "SettingsProvider: Failed to load themes")]
    public static partial IGenericMessage LoadThemesException(ILogger logger, Exception exception);

    [MessageLogging(EventId = 11004, Level = LogLevel.Trace,
        Message = "SettingsProvider: Updating setting '{settingName}'")]
    public static partial IGenericMessage UpdateSettingStarted(ILogger logger, string settingName);

    [MessageLogging(EventId = 11005, Level = LogLevel.Information,
        Message = "SettingsProvider: Updated setting '{settingName}'")]
    public static partial IGenericMessage UpdateSettingCompleted(ILogger logger, string settingName);

    [MessageLogging(EventId = 71004, Level = LogLevel.Warning,
        Message = "SettingsProvider: Failed to update setting '{settingName}'")]
    public static partial IGenericMessage UpdateSettingFailed(ILogger logger, string settingName);

    [MessageLogging(EventId = 71005, Level = LogLevel.Warning,
        Message = "SettingsProvider: Failed to update setting '{settingName}'")]
    public static partial IGenericMessage UpdateSettingException(ILogger logger, Exception exception, string settingName);

    [MessageLogging(EventId = 11006, Level = LogLevel.Trace,
        Message = "SettingsProvider: Setting default theme to '{themeName}'")]
    public static partial IGenericMessage SetThemeStarted(ILogger logger, string themeName);

    [MessageLogging(EventId = 11007, Level = LogLevel.Information,
        Message = "SettingsProvider: Set default theme to '{themeName}'")]
    public static partial IGenericMessage SetThemeCompleted(ILogger logger, string themeName);

    [MessageLogging(EventId = 71006, Level = LogLevel.Warning,
        Message = "SettingsProvider: Failed to set default theme to '{themeName}'")]
    public static partial IGenericMessage SetThemeFailed(ILogger logger, string themeName);

    [MessageLogging(EventId = 71007, Level = LogLevel.Warning,
        Message = "SettingsProvider: Failed to set default theme to '{themeName}'")]
    public static partial IGenericMessage SetThemeException(ILogger logger, Exception exception, string themeName);
}
