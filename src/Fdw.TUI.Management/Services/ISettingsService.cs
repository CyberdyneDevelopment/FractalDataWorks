namespace Fdw.TUI.Management.Services;

/// <summary>
/// Service for managing application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the current application settings.
    /// </summary>
    ApplicationSettings GetSettings();

    /// <summary>
    /// Saves the application settings.
    /// </summary>
    void SaveSettings(ApplicationSettings settings);

    /// <summary>
    /// Resets all settings to their default values.
    /// </summary>
    void ResetToDefaults();
}