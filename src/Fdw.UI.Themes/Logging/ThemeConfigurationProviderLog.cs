using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Themes.Logging;

/// <summary>
/// MessageLogging for ThemeConfigurationProvider operations.
/// EventId range: 11000-11004, 30100-30101, 40100-40101
/// </summary>
[MessageLoggingTypeCode("THEMES")]
public static partial class ThemeConfigurationProviderLog
{
    /// <summary>
    /// Logs that a single theme was loaded from the specified source.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="themeName">The name of the theme that was loaded.</param>
    /// <param name="source">The source the theme was loaded from.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "Theme '{themeName}' loaded from {source}")]
    public static partial IGenericMessage ThemeLoaded(ILogger logger, string themeName, string source);

    /// <summary>
    /// Logs that all available themes were loaded, reporting the total count.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="count">The number of themes that were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11001, Level = LogLevel.Debug,
        Message = "Loaded {count} themes")]
    public static partial IGenericMessage AllThemesLoaded(ILogger logger, int count);

    /// <summary>Logs that the default theme was marked on a theme that had no default before it.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="themeName">The theme now marked as the default.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11002, Level = LogLevel.Information,
        Message = "Theme '{themeName}' is now the default theme")]
    public static partial IGenericMessage DefaultThemeSet(ILogger logger, string themeName);

    /// <summary>Logs that the default theme moved from one theme to another.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="themeName">The theme now marked as the default.</param>
    /// <param name="previousName">The theme that was the default.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11003, Level = LogLevel.Information,
        Message = "Default theme moved from '{previousName}' to '{themeName}'")]
    public static partial IGenericMessage DefaultThemeMoved(ILogger logger, string themeName, string previousName);

    /// <summary>Logs that making a theme the default changed nothing: it already was.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="themeName">The theme that is the default.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11004, Level = LogLevel.Debug,
        Message = "Theme '{themeName}' is already the default theme")]
    public static partial IGenericMessage DefaultThemeUnchanged(ILogger logger, string themeName);

    /// <summary>Refuses a request that needs a default theme because none is marked.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 30100, Level = LogLevel.Warning,
        Message = "No theme is marked as the default",
        ClientMessage = "No theme is set as the default")]
    public static partial IGenericMessage NoDefaultTheme(ILogger logger);

    /// <summary>Refuses a request that names a theme that does not exist.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="themeName">The name that matched no theme.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 30101, Level = LogLevel.Warning,
        Message = "Theme '{themeName}' was not found",
        ClientMessage = "Theme '{themeName}' was not found")]
    public static partial IGenericMessage ThemeNotFound(ILogger logger, string themeName);

    /// <summary>Refuses to remove the default theme.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="themeName">The default theme.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 40100, Level = LogLevel.Warning,
        Message = "Theme '{themeName}' was not removed: it is the default theme",
        ClientMessage = "Theme '{themeName}' is the default theme and cannot be removed. Set another theme as the default first")]
    public static partial IGenericMessage CannotRemoveDefaultTheme(ILogger logger, string themeName);

    /// <summary>Refuses to remove a built-in theme.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="themeName">The built-in theme.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 40101, Level = LogLevel.Warning,
        Message = "Theme '{themeName}' was not removed: it is a built-in theme",
        ClientMessage = "Theme '{themeName}' is a built-in theme and cannot be removed")]
    public static partial IGenericMessage CannotRemoveBuiltInTheme(ILogger logger, string themeName);
}
