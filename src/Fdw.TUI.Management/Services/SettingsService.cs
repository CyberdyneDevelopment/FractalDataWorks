using System;
using System.IO;
using System.Text.Json;

namespace Fdw.TUI.Management.Services;

/// <summary>
/// File-based implementation of the settings service.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Fdw",
        "Management");

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private ApplicationSettings? _cachedSettings;

    /// <inheritdoc />
    public ApplicationSettings GetSettings()
    {
        if (_cachedSettings != null)
        {
            return _cachedSettings;
        }

        if (!File.Exists(SettingsFilePath))
        {
            _cachedSettings = new ApplicationSettings();
            return _cachedSettings;
        }

        try
        {
            var json = File.ReadAllText(SettingsFilePath);
            _cachedSettings = JsonSerializer.Deserialize<ApplicationSettings>(json, JsonOptions)
                ?? new ApplicationSettings();
            return _cachedSettings;
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FDW] Settings file contains invalid JSON, using defaults: {ex.Message}");
            _cachedSettings = new ApplicationSettings();
            return _cachedSettings;
        }
    }

    /// <inheritdoc />
    public void SaveSettings(ApplicationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Directory.CreateDirectory(SettingsDirectory);

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsFilePath, json);

        _cachedSettings = settings;
    }

    /// <inheritdoc />
    public void ResetToDefaults()
    {
        var defaults = new ApplicationSettings();
        SaveSettings(defaults);
    }
}
