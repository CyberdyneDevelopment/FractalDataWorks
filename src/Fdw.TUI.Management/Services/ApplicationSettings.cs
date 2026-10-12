namespace Fdw.TUI.Management.Services;

/// <summary>
/// Application settings model.
/// </summary>
public sealed class ApplicationSettings
{
    /// <summary>
    /// Gets or sets the theme name.
    /// </summary>
    public string ThemeName { get; set; } = "Default";

    /// <summary>
    /// Gets or sets the minimum log level to display.
    /// </summary>
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// Gets or sets whether to automatically connect on startup.
    /// </summary>
    public bool AutoConnectOnStartup { get; set; }

    /// <summary>
    /// Gets or sets whether to confirm before exiting.
    /// </summary>
    public bool ConfirmOnExit { get; set; } = true;

    /// <summary>
    /// Gets or sets the default page size for list views.
    /// </summary>
    public int DefaultPageSize { get; set; } = 25;

    /// <summary>
    /// Gets or sets the connection timeout in seconds.
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;
}