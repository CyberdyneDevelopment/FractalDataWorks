using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Components.Logging;

/// <summary>
/// MessageLogging for ProfileProvider operations.
/// EventId range: 4450-4464
/// </summary>
[MessageLoggingTypeCode("COMPONENTS5")]
public static partial class ProfileProviderLog
{
    /// <summary>
    /// Logs that the user profile is being loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Trace,
        Message = "Loading user profile")]
    public static partial IGenericMessage LoadingProfile(ILogger logger);

    /// <summary>
    /// Logs that the profile for the given user was loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="username">The user name whose profile was loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11012,
        Level = LogLevel.Debug,
        Message = "Loaded profile for user '{username}'")]
    public static partial IGenericMessage LoadedProfile(ILogger logger, string username);

    /// <summary>
    /// Logs that loading the user profile failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71006,
        Level = LogLevel.Error,
        Message = "Failed to load user profile")]
    public static partial IGenericMessage LoadProfileFailed(ILogger logger);

    /// <summary>
    /// Logs that the given number of user preferences were loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="count">The number of user preferences loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11013,
        Level = LogLevel.Debug,
        Message = "Loaded {count} user preferences")]
    public static partial IGenericMessage LoadedPreferences(ILogger logger, int count);

    /// <summary>
    /// Logs that loading user preferences failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71007,
        Level = LogLevel.Error,
        Message = "Failed to load user preferences")]
    public static partial IGenericMessage LoadPreferencesFailed(ILogger logger);

    /// <summary>
    /// Logs that the user password is being changed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11014,
        Level = LogLevel.Trace,
        Message = "Changing user password")]
    public static partial IGenericMessage ChangingPassword(ILogger logger);

    /// <summary>
    /// Logs that the user password was changed successfully.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11015,
        Level = LogLevel.Information,
        Message = "Password changed successfully")]
    public static partial IGenericMessage PasswordChanged(ILogger logger);

    /// <summary>
    /// Logs that changing the user password failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71008,
        Level = LogLevel.Error,
        Message = "Failed to change password")]
    public static partial IGenericMessage ChangePasswordFailed(ILogger logger);

    /// <summary>
    /// Logs that an exception occurred while changing the user password.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that occurred while changing the password.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91002,
        Level = LogLevel.Error,
        Message = "Exception changing password")]
    public static partial IGenericMessage ChangePasswordException(ILogger logger, Exception exception);

    /// <summary>
    /// Logs that the preference with the given key is being set.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="key">The key of the preference being set.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11016,
        Level = LogLevel.Trace,
        Message = "Setting preference '{key}'")]
    public static partial IGenericMessage SettingPreference(ILogger logger, string key);

    /// <summary>
    /// Logs that the preference with the given key was set successfully.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="key">The key of the preference that was set.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11017,
        Level = LogLevel.Debug,
        Message = "Preference '{key}' set successfully")]
    public static partial IGenericMessage PreferenceSet(ILogger logger, string key);

    /// <summary>
    /// Logs that setting the preference with the given key failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="key">The key of the preference that failed to be set.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71009,
        Level = LogLevel.Error,
        Message = "Failed to set preference '{key}'")]
    public static partial IGenericMessage SetPreferenceFailed(ILogger logger, string key);
}
