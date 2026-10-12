using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Fdw.TUI.Management.Navigation;
using Fdw.TUI.Management.Services;
using Fdw.UI.Themes;
using Spectre.Console;

namespace Fdw.TUI.Management.Screens;

/// <summary>
/// Settings screen for application preferences and configuration.
/// </summary>
public sealed class SettingsScreen : ScreenBase
{
    private readonly ISettingsService _settingsService;

    /// <inheritdoc />
    public override string Title => "Application Settings";

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsScreen"/> class.
    /// </summary>
    public SettingsScreen(
        IAnsiConsole console,
        IMenuTheme theme,
        IScreenFactory screenFactory,
        ISettingsService settingsService)
        : base(console, theme, screenFactory)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
    }

    /// <inheritdoc />
    public override Task<NavigationResult> Show()
    {
        RenderHeader();

        var settings = _settingsService.GetSettings();

        // Display current settings
        RenderCurrentSettings(settings);

        // Build menu choices
        var choices = new List<(string Id, string Label)>
        {
            ("theme", $"Theme: {settings.ThemeName}"),
            ("log_level", $"Log Level: {settings.LogLevel}"),
            ("auto_connect", $"Auto-connect on startup: {(settings.AutoConnectOnStartup ? "Yes" : "No")}"),
            ("confirm_exit", $"Confirm on exit: {(settings.ConfirmOnExit ? "Yes" : "No")}"),
            ("page_size", $"Default page size: {settings.DefaultPageSize.ToString(CultureInfo.InvariantCulture)}"),
            ("timeout", $"Connection timeout: {settings.ConnectionTimeoutSeconds.ToString(CultureInfo.InvariantCulture)}s"),
            ("reset", "Reset to defaults"),
            ("back", "Back")
        };

        var prompt = new SelectionPrompt<(string Id, string Label)>()
            .Title($"[{Theme.Colors.Primary}]Select setting to modify:[/]")
            .AddChoices(choices)
            .UseConverter(c => c.Label)
            .HighlightStyle(new Style(Theme.Colors.Selected));

        var selected = Console.Prompt(prompt);

        return Task.FromResult(HandleSelection(selected.Id, settings));
    }

    private void RenderCurrentSettings(ApplicationSettings settings)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Theme.Colors.Muted)
            .Title($"[{Theme.Colors.Secondary}]Current Settings[/]");

        table.AddColumn(new TableColumn($"[{Theme.Colors.Primary}]Setting[/]").LeftAligned());
        table.AddColumn(new TableColumn($"[{Theme.Colors.Primary}]Value[/]").LeftAligned());

        table.AddRow("Theme", $"[{Theme.Colors.Info}]{settings.ThemeName}[/]");
        table.AddRow("Log Level", $"[{Theme.Colors.Info}]{settings.LogLevel}[/]");
        table.AddRow("Auto-connect", settings.AutoConnectOnStartup
            ? $"[{Theme.Colors.Success}]Yes[/]"
            : $"[{Theme.Colors.Muted}]No[/]");
        table.AddRow("Confirm on exit", settings.ConfirmOnExit
            ? $"[{Theme.Colors.Success}]Yes[/]"
            : $"[{Theme.Colors.Muted}]No[/]");
        table.AddRow("Default page size", $"[{Theme.Colors.Info}]{settings.DefaultPageSize.ToString(CultureInfo.InvariantCulture)}[/]");
        table.AddRow("Connection timeout", $"[{Theme.Colors.Info}]{settings.ConnectionTimeoutSeconds.ToString(CultureInfo.InvariantCulture)}s[/]");

        Console.Write(table);
        Console.WriteLine();
    }

    private NavigationResult HandleSelection(string id, ApplicationSettings settings)
    {
        // NOTE: Consider using dispatcher pattern with type collection for better extensibility
        switch (id)
        {
            case "theme":
                EditTheme(settings);
                break;

            case "log_level":
                EditLogLevel(settings);
                break;

            case "auto_connect":
                settings.AutoConnectOnStartup = !settings.AutoConnectOnStartup;
                _settingsService.SaveSettings(settings);
                RenderSuccess($"Auto-connect {(settings.AutoConnectOnStartup ? "enabled" : "disabled")}");
                break;

            case "confirm_exit":
                settings.ConfirmOnExit = !settings.ConfirmOnExit;
                _settingsService.SaveSettings(settings);
                RenderSuccess($"Confirm on exit {(settings.ConfirmOnExit ? "enabled" : "disabled")}");
                break;

            case "page_size":
                EditPageSize(settings);
                break;

            case "timeout":
                EditTimeout(settings);
                break;

            case "reset":
                if (Console.Confirm($"[{Theme.Colors.Warning}]Reset all settings to defaults?[/]", false))
                {
                    _settingsService.ResetToDefaults();
                    RenderSuccess("Settings reset to defaults");
                }
                break;

            case "back":
                return NavigationResult.Pop();
        }

        return NavigationResult.Stay();
    }

    private void EditTheme(ApplicationSettings settings)
    {
        var themes = new List<string> { "Default", "Dark", "Light", "High Contrast" };

        var prompt = new SelectionPrompt<string>()
            .Title($"[{Theme.Colors.Primary}]Select theme:[/]")
            .AddChoices(themes)
            .HighlightStyle(new Style(Theme.Colors.Selected));

        var selected = Console.Prompt(prompt);
        settings.ThemeName = selected;
        _settingsService.SaveSettings(settings);
        RenderSuccess($"Theme changed to {selected}");
    }

    private void EditLogLevel(ApplicationSettings settings)
    {
        var levels = new List<string> { "Trace", "Debug", "Information", "Warning", "Error", "Critical" };

        var prompt = new SelectionPrompt<string>()
            .Title($"[{Theme.Colors.Primary}]Select minimum log level:[/]")
            .AddChoices(levels)
            .HighlightStyle(new Style(Theme.Colors.Selected));

        var selected = Console.Prompt(prompt);
        settings.LogLevel = selected;
        _settingsService.SaveSettings(settings);
        RenderSuccess($"Log level changed to {selected}");
    }

    private void EditPageSize(ApplicationSettings settings)
    {
        var sizes = new List<int> { 10, 25, 50, 100, 250 };

        var prompt = new SelectionPrompt<int>()
            .Title($"[{Theme.Colors.Primary}]Select default page size:[/]")
            .AddChoices(sizes)
            .UseConverter(s => s.ToString(CultureInfo.InvariantCulture))
            .HighlightStyle(new Style(Theme.Colors.Selected));

        var selected = Console.Prompt(prompt);
        settings.DefaultPageSize = selected;
        _settingsService.SaveSettings(settings);
        RenderSuccess($"Default page size changed to {selected.ToString(CultureInfo.InvariantCulture)}");
    }

    private void EditTimeout(ApplicationSettings settings)
    {
        var timeouts = new List<int> { 15, 30, 60, 120, 300 };

        var prompt = new SelectionPrompt<int>()
            .Title($"[{Theme.Colors.Primary}]Select connection timeout (seconds):[/]")
            .AddChoices(timeouts)
            .UseConverter(t => $"{t.ToString(CultureInfo.InvariantCulture)}s")
            .HighlightStyle(new Style(Theme.Colors.Selected));

        var selected = Console.Prompt(prompt);
        settings.ConnectionTimeoutSeconds = selected;
        _settingsService.SaveSettings(settings);
        RenderSuccess($"Connection timeout changed to {selected.ToString(CultureInfo.InvariantCulture)}s");
    }
}
