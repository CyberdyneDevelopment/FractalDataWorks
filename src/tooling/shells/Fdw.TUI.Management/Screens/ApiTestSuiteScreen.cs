using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Fdw.TUI.Management.Navigation;
using Fdw.TUI.Management.Services;
using Fdw.UI.Themes;
using Spectre.Console;

namespace Fdw.TUI.Management.Screens;

/// <summary>
/// Navigate, run, evaluate and refresh the generated API test suite.
/// </summary>
/// <remarks>
/// The suite exists because a dead button in the UI is almost never a UI bug — it is a route
/// that was never mapped, an endpoint that faulted, or an authorisation failure the page
/// swallowed. This screen is how an operator asks which of those happened without leaving
/// the tool.
/// </remarks>
public sealed class ApiTestSuiteScreen : ScreenBase
{
    private readonly INewmanSuiteService suite;

    /// <inheritdoc />
    public override string Title => "API Test Suite";

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiTestSuiteScreen"/> class.
    /// </summary>
    /// <param name="console">The console.</param>
    /// <param name="theme">The theme.</param>
    /// <param name="screenFactory">The screen factory.</param>
    /// <param name="suite">The suite service.</param>
    public ApiTestSuiteScreen(
        IAnsiConsole console,
        IMenuTheme theme,
        IScreenFactory screenFactory,
        INewmanSuiteService suite)
        : base(console, theme, screenFactory)
    {
        this.suite = suite ?? throw new ArgumentNullException(nameof(suite));
    }

    /// <inheritdoc />
    public override async Task<NavigationResult> Show()
    {
        RenderHeader();

        var folders = await suite.GetFolders().ConfigureAwait(false);
        if (folders.IsFailure)
        {
            RenderWarning(folders.CurrentMessage ?? "The suite could not be read.");
            Console.WriteLine();

            if (Console.Confirm($"[{Theme.Colors.Primary}]Pull the OpenAPI document and generate it now?[/]", true))
            {
                return await Refresh().ConfigureAwait(false);
            }

            return NavigationResult.Pop();
        }

        RenderCoverage(folders.Value ?? Array.Empty<NewmanFolder>());

        var choices = new List<(string Id, string Label)>
        {
            ("run-all", "Run the whole suite"),
            ("run-folder", "Run one domain"),
            ("failures", "Show what failed last run"),
            ("refresh", "Refresh from the API's OpenAPI document"),
            ("back", "Back"),
        };

        var selected = Console.Prompt(
            new SelectionPrompt<(string Id, string Label)>()
                .Title($"[{Theme.Colors.Primary}]Select an action:[/]")
                .AddChoices(choices)
                .UseConverter(c => c.Label)
                .HighlightStyle(new Style(Theme.Colors.Selected)));

        return selected.Id switch
        {
            "run-all" => await RunAndReport(null).ConfigureAwait(false),
            "run-folder" => await RunFolder(folders.Value ?? Array.Empty<NewmanFolder>()).ConfigureAwait(false),
            "failures" => await ShowFailures().ConfigureAwait(false),
            "refresh" => await Refresh().ConfigureAwait(false),
            _ => NavigationResult.Pop(),
        };
    }

    private void RenderCoverage(IReadOnlyList<NewmanFolder> folders)
    {
        var total = folders.Sum(f => f.RequestCount);
        Console.MarkupLine(
            $"[{Theme.Colors.Muted}]{folders.Count} domain(s), {total.ToString(CultureInfo.InvariantCulture)} request(s) generated from the API's OpenAPI document[/]");
        Console.WriteLine();

        var table = new Table()
            .Border(TableBorder.Simple)
            .BorderColor(Theme.Colors.Muted);
        table.AddColumn(new TableColumn($"[{Theme.Colors.Primary}]Domain[/]").LeftAligned());
        table.AddColumn(new TableColumn($"[{Theme.Colors.Primary}]Requests[/]").RightAligned());

        foreach (var folder in folders.OrderByDescending(f => f.RequestCount))
        {
            table.AddRow(
                Markup.Escape(folder.Name),
                $"[{Theme.Colors.Muted}]{folder.RequestCount.ToString(CultureInfo.InvariantCulture)}[/]");
        }

        Console.Write(table);
        Console.WriteLine();
    }

    private async Task<NavigationResult> RunFolder(IReadOnlyList<NewmanFolder> folders)
    {
        if (folders.Count == 0)
        {
            RenderWarning("The collection holds no domains to run.");
            return Pause();
        }

        var picked = Console.Prompt(
            new SelectionPrompt<NewmanFolder>()
                .Title($"[{Theme.Colors.Primary}]Which domain?[/]")
                .AddChoices(folders.OrderBy(f => f.Name, StringComparer.Ordinal))
                .UseConverter(f => $"{f.Name} ({f.RequestCount.ToString(CultureInfo.InvariantCulture)})")
                .HighlightStyle(new Style(Theme.Colors.Selected)));

        return await RunAndReport(picked.Name).ConfigureAwait(false);
    }

    private async Task<NavigationResult> RunAndReport(string? folder)
    {
        Console.WriteLine();
        Console.MarkupLine($"[{Theme.Colors.Info}]Running {Markup.Escape(folder ?? "the whole suite")}…[/]");
        Console.WriteLine();

        // The runner streams its own output to this terminal, so there is no spinner here —
        // hiding a long run behind a spinner would hide the only progress there is.
        var run = await suite.Run(folder).ConfigureAwait(false);
        Console.WriteLine();

        if (run.IsFailure)
        {
            RenderWarning(run.CurrentMessage ?? "The suite could not be run.");
            return Pause();
        }

        var result = run.Value!;
        var summary =
            $"{result.Requests.ToString(CultureInfo.InvariantCulture)} request(s), " +
            $"{result.Assertions.ToString(CultureInfo.InvariantCulture)} assertion(s), " +
            $"{result.Failures.ToString(CultureInfo.InvariantCulture)} failure(s) " +
            $"in {result.DurationMs.ToString(CultureInfo.InvariantCulture)}ms";

        if (result.Passed)
        {
            RenderSuccess(summary);
            return Pause();
        }

        RenderWarning(summary);
        Console.WriteLine();
        await RenderFailures().ConfigureAwait(false);
        return Pause();
    }

    private async Task<NavigationResult> ShowFailures()
    {
        Console.WriteLine();
        await RenderFailures().ConfigureAwait(false);
        return Pause();
    }

    private async Task RenderFailures()
    {
        var failures = await suite.GetLastFailures().ConfigureAwait(false);
        if (failures.IsFailure)
        {
            RenderWarning(failures.CurrentMessage ?? "The last run could not be read.");
            return;
        }

        var list = failures.Value ?? Array.Empty<NewmanFailure>();
        if (list.Count == 0)
        {
            RenderSuccess("Every assertion in the last run passed.");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Simple)
            .BorderColor(Theme.Colors.Muted);
        table.AddColumn(new TableColumn($"[{Theme.Colors.Primary}]Request[/]").LeftAligned());
        table.AddColumn(new TableColumn($"[{Theme.Colors.Primary}]Assertion[/]").LeftAligned());
        table.AddColumn(new TableColumn($"[{Theme.Colors.Primary}]Detail[/]").LeftAligned());

        foreach (var failure in list)
        {
            table.AddRow(
                $"[{Theme.Colors.Warning}]{Markup.Escape(failure.Request)}[/]",
                Markup.Escape(failure.Assertion),
                $"[{Theme.Colors.Muted}]{Markup.Escape(Shorten(failure.Detail))}[/]");
        }

        Console.Write(table);
        Console.WriteLine();
        Console.MarkupLine(
            $"[{Theme.Colors.Muted}]A 404 on 'requires auth' means the route is not mapped. A 5xx means the API's own log has a stack.[/]");
    }

    private async Task<NavigationResult> Refresh()
    {
        Console.WriteLine();
        var refreshed = await suite.Refresh().ConfigureAwait(false);
        Console.WriteLine();

        if (refreshed.IsFailure)
        {
            RenderWarning(refreshed.CurrentMessage ?? "The suite could not be refreshed.");
            return Pause();
        }

        var r = refreshed.Value!;
        RenderSuccess(
            $"{r.Paths.ToString(CultureInfo.InvariantCulture)} path(s), " +
            $"{r.Operations.ToString(CultureInfo.InvariantCulture)} operation(s) → " +
            $"{r.Requests.ToString(CultureInfo.InvariantCulture)} generated request(s)");
        return Pause();
    }

    private static string Shorten(string value) =>
        value.Length <= 90 ? value : string.Concat(value.AsSpan(0, 87), "…");

    private NavigationResult Pause()
    {
        Console.WriteLine();
        Console.MarkupLine($"[{Theme.Colors.Muted}]Press any key to continue...[/]");
        System.Console.ReadKey(true);
        return NavigationResult.Stay();
    }
}
