using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Themes.Components.Themes;

/// <summary>
/// MessageLogging for BlazorThemeProvider operations.
/// EventId range: 7200-7209
/// </summary>
[MessageLoggingTypeCode("THEMESCOMPONENTS")]
public static partial class BlazorThemeProviderLog
{
    /// <summary>
    /// Logs that the BlazorThemeProvider was initialized with the specified theme.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="themeName">The name of the theme the provider was initialized with.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Trace,
        Message = "BlazorThemeProvider initialized with theme '{themeName}'")]
    public static partial IGenericMessage Initialized(ILogger logger, string themeName);

    /// <summary>
    /// Logs that the active Blazor theme was switched to the specified theme.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="themeName">The name of the theme that was switched to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Information,
        Message = "Switched Blazor theme to '{themeName}'")]
    public static partial IGenericMessage ThemeSwitched(ILogger logger, string themeName);

    /// <summary>
    /// Logs that the requested theme was not found among the registered themes.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="themeName">The name of the theme that could not be found.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 31000,
        Level = LogLevel.Warning,
        Message = "Theme '{themeName}' not found in registered themes")]
    public static partial IGenericMessage ThemeNotFound(ILogger logger, string themeName);
}
