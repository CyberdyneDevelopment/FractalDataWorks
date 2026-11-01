using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.TUI.Management;

/// <summary>
/// MessageLogging for the management console's startup.
/// EventId range: 9760-9769.
/// </summary>
/// <remarks>
/// Each message is also the failure <c>Main</c> prints before it exits with 1, so the operator reads
/// the same text the log holds.
/// </remarks>
[ExcludeFromCodeCoverage]
public static partial class TuiStartupLog
{
    /// <summary>Logs that no menu theme is configured.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The message.</returns>
    [MessageLogging(
        EventId = 9760,
        Level = LogLevel.Error,
        Message = "No menu theme is configured. Set Tui:Theme to a MenuThemes name (Dark, Light or HighContrast)")]
    public static partial IGenericMessage MenuThemeNotConfigured(ILogger logger);

    /// <summary>Logs a configured menu theme that names no MenuThemes option.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="themeName">The configured name.</param>
    /// <returns>The message.</returns>
    [MessageLogging(
        EventId = 9761,
        Level = LogLevel.Error,
        Message = "Tui:Theme '{themeName}' is not a MenuThemes name (Dark, Light or HighContrast)")]
    public static partial IGenericMessage MenuThemeNotFound(ILogger logger, string themeName);
}
