using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Themes.Logging;

/// <summary>
/// MessageLogging for ThemeConfigurationProvider operations.
/// EventId range: 9420-9424
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
}
