using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.TUI.Management.Navigation;
using Fdw.TUI.Management.Services;
using Fdw.UI.Themes;
using Spectre.Console;

namespace Fdw.TUI.Management.Screens;

/// <summary>
/// Menu screen for monitoring and log viewing options.
/// </summary>
public sealed class MonitoringMenuScreen : ScreenBase
{
    private readonly IConnectionManager _connectionManager;

    /// <inheritdoc />
    public override string Title => "Monitoring & Logs";

    /// <summary>
    /// Initializes a new instance of the <see cref="MonitoringMenuScreen"/> class.
    /// </summary>
    public MonitoringMenuScreen(
        IAnsiConsole console,
        IMenuTheme theme,
        IScreenFactory screenFactory,
        IConnectionManager connectionManager)
        : base(console, theme, screenFactory)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
    }

    /// <inheritdoc />
    // MA0051: Method length acceptable - procedural menu rendering with connection check, table display, and prompt
#pragma warning disable MA0051 // Method is too long
    public override Task<NavigationResult> Show()
#pragma warning restore MA0051
    {
        RenderHeader();

        // Check connection status
        var status = _connectionManager.GetStatus();
        if (!status.IsConnected)
        {
            RenderWarning("Not connected to any instance. Please connect first.");
            Console.WriteLine();

            var connectChoice = Console.Confirm($"[{Theme.Colors.Primary}]Go to connections?[/]", true);
            if (connectChoice)
            {
                return Task.FromResult(NavigationResult.Push(ScreenFactory.Create<ConnectionsScreen>()));
            }
            return Task.FromResult(NavigationResult.Pop());
        }

        Console.MarkupLine($"[{Theme.Colors.Muted}]Connected to: {Markup.Escape(status.InstanceName ?? "Unknown")}[/]");
        Console.WriteLine();

        var choices = new List<(string Id, string Label, string Description)>
        {
            ("dashboard", "System Dashboard", "View service health and metrics"),
            ("logs", "Log Viewer", "View and filter application logs"),
            ("pipelines", "Pipeline History", "View pipeline execution history"),
            ("workflows", "Workflow History", "View workflow execution history"),
            ("back", "Back", "Return to main menu")
        };

        // Render choices description
        var table = new Table()
            .Border(TableBorder.Simple)
            .BorderColor(Theme.Colors.Muted)
            .HideHeaders();

        table.AddColumn(new TableColumn("").LeftAligned().Width(20));
        table.AddColumn(new TableColumn("").LeftAligned());

        foreach (var choice in choices)
        {
            if (string.Equals(choice.Id, "back", StringComparison.Ordinal))
            {
                continue;
            }
            table.AddRow(
                $"[{Theme.Colors.Primary}]{choice.Label}[/]",
                $"[{Theme.Colors.Muted}]{choice.Description}[/]"
            );
        }

        Console.Write(table);
        Console.WriteLine();

        var prompt = new SelectionPrompt<(string Id, string Label, string Description)>()
            .Title($"[{Theme.Colors.Primary}]Select monitoring option:[/]")
            .AddChoices(choices)
            .UseConverter(c => c.Label)
            .HighlightStyle(new Style(Theme.Colors.Selected));

        var selected = Console.Prompt(prompt);

        return Task.FromResult(HandleSelection(selected.Id));
    }

    private NavigationResult HandleSelection(string id)
    {
        return id switch
        {
            "dashboard" => NavigationResult.Push(ScreenFactory.Create<DashboardScreen>()),
            "logs" => ShowLogViewer(),
            "pipelines" => ShowExecutionHistory("Pipeline"),
            "workflows" => ShowExecutionHistory("Workflow"),
            "back" => NavigationResult.Pop(),
            _ => NavigationResult.Stay()
        };
    }

    private NavigationResult ShowLogViewer()
    {
        Console.WriteLine();
        Console.MarkupLine($"[{Theme.Colors.Info}]Log Viewer[/]");
        Console.MarkupLine($"[{Theme.Colors.Muted}]This feature will display logs using the LogViewerPageRenderer.[/]");
        Console.WriteLine();
        Console.MarkupLine($"[{Theme.Colors.Muted}]Press any key to continue...[/]");
        System.Console.ReadKey(true);

        return NavigationResult.Stay();
    }

    private NavigationResult ShowExecutionHistory(string entityType)
    {
        Console.WriteLine();
        Console.MarkupLine($"[{Theme.Colors.Info}]{entityType} Execution History[/]");
        Console.MarkupLine($"[{Theme.Colors.Muted}]This feature will display {entityType.ToLowerInvariant()} history using the ListPageRenderer.[/]");
        Console.WriteLine();
        Console.MarkupLine($"[{Theme.Colors.Muted}]Press any key to continue...[/]");
        System.Console.ReadKey(true);

        return NavigationResult.Stay();
    }
}
