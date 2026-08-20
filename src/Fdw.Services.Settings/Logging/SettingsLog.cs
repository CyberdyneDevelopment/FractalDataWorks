using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Logging;

/// <summary>
/// High-performance MessageLogging for settings service operations.
/// EventId range: 7000-7099
/// </summary>
[MessageLoggingTypeCode("SETTINGS")]
public static partial class SettingsLog
{
    /// <summary>
    /// Logs when a setting is resolved at server level.
    /// </summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Trace,
        Message = "Setting '{settingName}' resolved at server level with value '{settingValue}'")]
    public static partial IGenericMessage SettingResolvedAtServerLevel(
        ILogger logger,
        string settingName,
        string settingValue);

    /// <summary>
    /// Logs when a setting is overridden at tenant level.
    /// </summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Trace,
        Message = "Setting '{settingName}' overridden at tenant level for tenant '{tenantId}'")]
    public static partial IGenericMessage SettingOverriddenAtTenantLevel(
        ILogger logger,
        string settingName,
        string tenantId);

    /// <summary>
    /// Logs when a setting is overridden at role level.
    /// </summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Trace,
        Message = "Setting '{settingName}' overridden at role level for role '{roleName}' in tenant '{tenantId}'")]
    public static partial IGenericMessage SettingOverriddenAtRoleLevel(
        ILogger logger,
        string settingName,
        string roleName,
        string tenantId);

    /// <summary>
    /// Logs when a server setting is not found.
    /// </summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Warning,
        Message = "Server setting '{settingName}' not found")]
    public static partial IGenericMessage ServerSettingNotFound(
        ILogger logger,
        string settingName);

    /// <summary>
    /// Logs when a setting value is clamped to the minimum bound.
    /// </summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Information,
        Message = "Setting '{settingName}' value '{rawValue}' clamped to minimum '{clampedValue}' (min: {minValue})")]
    public static partial IGenericMessage SettingClampedToMin(
        ILogger logger,
        string settingName,
        string rawValue,
        string clampedValue,
        string minValue);

    /// <summary>
    /// Logs when a setting value is clamped to the maximum bound.
    /// </summary>
    // Why Information: a configured value did NOT take effect as written. Silent clamping is exactly the
    // surprise an operator must be able to see without raising verbosity.
    [MessageLogging(EventId = 11008, Level = LogLevel.Information,
        Message = "Setting '{settingName}' value '{rawValue}' clamped to maximum '{clampedValue}' (max: {maxValue})")]
    public static partial IGenericMessage SettingClampedToMax(
        ILogger logger,
        string settingName,
        string rawValue,
        string clampedValue,
        string maxValue);

    /// <summary>
    /// Logs when type conversion fails for a setting value.
    /// </summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error,
        Message = "Failed to convert setting '{settingName}' value '{settingValue}' to type '{targetType}'")]
    public static partial IGenericMessage SettingConversionFailed(
        ILogger logger,
        string settingName,
        string settingValue,
        string targetType);

    /// <summary>
    /// Logs when a setting is not active.
    /// </summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Debug,
        Message = "Setting '{settingName}' is not active, skipping")]
    public static partial IGenericMessage SettingNotActive(
        ILogger logger,
        string settingName);

    /// <summary>
    /// Logs when type conversion fails for a setting value due to an exception.
    /// </summary>
    [MessageLogging(EventId = 91001, Level = LogLevel.Error,
        Message = "Failed to convert setting '{settingName}' value '{settingValue}' to type '{targetType}': {error}")]
    public static partial IGenericMessage SettingConversionException(
        ILogger logger,
        Exception exception,
        string settingName,
        string settingValue,
        string targetType,
        string error);
}
