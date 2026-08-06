using System;
using System.Linq;
using System.Threading.Tasks;
using Fdw.TUI.Management.Navigation;
using Fdw.TUI.Management.Services;
using Fdw.UI.Themes;
using Spectre.Console;

namespace Fdw.TUI.Management.Screens;

/// <summary>
/// Main menu screen for the management application.
/// Uses the Dispatch pattern via MenuTargets TypeCollection.
/// </summary>
public sealed class MainMenuScreen : ScreenBase
{
    private readonly IConnectionManager _connectionManager;

    /// <inheritdoc />
    public override string Title => "Fdw Management";

    /// <summary>
    /// Initializes a new instance of the <see cref="MainMenuScreen"/> class.
    /// </summary>
    public MainMenuScreen(
        IAnsiConsole console,
        IMenuTheme theme,
        IScreenFactory screenFactory,
        IConnectionManager connectionManager)
        : base(console, theme, screenFactory)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
    }

    /// <inheritdoc />
    public override Task<NavigationResult> Show()
    {
        RenderHeader();

        // Show connection status
        RenderConnectionStatus();

        // Build menu choices from TypeCollection - dispatch pattern
        var isConnected = _connectionManager.GetStatus().IsConnected;
        var availableTargets = MenuTargets.All()
            .Where(t => t.IsAvailable && (!t.RequiresConnection || isConnected))
            .OrderBy(t => t.Group, StringComparer.Ordinal)
            .ThenBy(t => t.Order)
            .Select(t => (t.Name, t.Label))
            .ToList();

        var prompt = new SelectionPrompt<(string Name, string Label)>()
            .Title($"[{Theme.Colors.Primary}]Select an option:[/]")
            .AddChoices(availableTargets)
            .UseConverter(c => c.Label)
            .HighlightStyle(new Style(Theme.Colors.Selected));

        var selected = Console.Prompt(prompt);

        // Dispatch to the selected menu target - no switch statement needed
        return Task.FromResult(HandleSelection(selected.Name));
    }

    private void RenderConnectionStatus()
    {
        var status = _connectionManager.GetStatus();

        if (status.IsConnected)
        {
            Console.MarkupLine($"[{Theme.Colors.Success}]{Theme.Icons.SuccessIcon} Connected to: {Markup.Escape(status.InstanceName ?? "Unknown")}[/]");
            Console.MarkupLine($"[{Theme.Colors.Muted}]   URL: {Markup.Escape(status.Url ?? "N/A")}[/]");
        }
        else
        {
            Console.MarkupLine($"[{Theme.Colors.Warning}]{Theme.Icons.WarningIcon} Not connected to any instance[/]");
        }

        Console.WriteLine();
    }

    private NavigationResult HandleSelection(string id)
    {
        // Dispatch pattern - each MenuTarget knows its own navigation behavior
        var target = MenuTargets.ByName(id);

        if (target == MenuTargets.NotFound)
        {
            return NavigationResult.Stay();
        }

        return target.Navigate(ScreenFactory);
    }
}
