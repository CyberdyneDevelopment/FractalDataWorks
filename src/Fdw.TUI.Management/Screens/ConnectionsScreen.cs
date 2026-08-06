using System;
using System.Globalization;
using System.Threading.Tasks;
using Fdw.TUI.Management.Navigation;
using Fdw.TUI.Management.Services;
using Fdw.UI.Abstractions.Pages;
using Fdw.UI.Abstractions.Rendering;
using Fdw.UI.Components.Pages;
using Fdw.UI.Themes;
using Spectre.Console;

namespace Fdw.TUI.Management.Screens;

/// <summary>
/// Screen for managing connections to Fdw instances.
/// </summary>
/// <remarks>
/// The list surface of this screen is expressed as a render-agnostic <see cref="ListPageModel"/>
/// (columns, rows, actions) rather than hand-rolled console output, and it is painted through the
/// render-agnostic seam (<see cref="IUIRenderer"/> + <see cref="IRenderContext"/>). The screen names
/// no rendering backend — the composition root chooses which registered renderer paints it.
/// </remarks>
public sealed class ConnectionsScreen : ScreenBase
{
    private const string ColumnName = "name";
    private const string ColumnUrl = "url";
    private const string ColumnLastUsed = "lastUsed";

    private const string ActionNew = "new";
    private const string ActionQuickConnect = "quick";
    private const string ActionDisconnect = "disconnect";

    private readonly IConnectionManager _connectionManager;
    private readonly IUIRenderer _renderer;
    private readonly IRenderContext _renderContext;

    /// <inheritdoc />
    public override string Title => "Instance Connections";

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionsScreen"/> class.
    /// </summary>
    public ConnectionsScreen(
        IAnsiConsole console,
        IMenuTheme theme,
        IScreenFactory screenFactory,
        IConnectionManager connectionManager,
        IUIRenderer renderer,
        IRenderContext renderContext)
        : base(console, theme, screenFactory)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _renderContext = renderContext ?? throw new ArgumentNullException(nameof(renderContext));
    }

    /// <inheritdoc />
    public override async Task<NavigationResult> Show()
    {
        while (true)
        {
            // Why: the renderer owns the header, table, pagination and the action prompt — including its
            // own "Back" choice, which it signals via ShouldExit. The screen therefore contributes only
            // the page's data and its domain actions, never console output.
            var result = await _renderer.RenderListPage(BuildPage(), _renderContext).ConfigureAwait(false);

            // Why: fail loud — a renderer that could not paint the page must not look like "user went back".
            if (!result.Success)
            {
                RenderStatus(result.Error ?? "The connections page could not be rendered.", isError: true);
                return NavigationResult.Pop();
            }

            if (result.ShouldExit && result.Action is null)
                return NavigationResult.Pop();

            switch (result.Action?.Id)
            {
                case ActionNew:
                    await AddNewConnection().ConfigureAwait(false);
                    break;

                case ActionQuickConnect:
                    await QuickConnect().ConfigureAwait(false);
                    break;

                case ActionDisconnect:
                    _connectionManager.Disconnect();
                    RenderSuccess("Disconnected from instance.");
                    await Task.Delay(1000).ConfigureAwait(false);
                    break;
            }
        }
    }

    /// <summary>
    /// Builds the render-agnostic page model for the connections list.
    /// </summary>
    private ListPageModel BuildPage()
    {
        var status = _connectionManager.GetStatus();
        var savedConnections = _connectionManager.GetSavedConnections();

        var page = new ListPageModel
        {
            Id = "connections",
            Title = Title,
            EntityTypeName = "Connection",
            Description = status.IsConnected ? $"Currently connected to: {status.InstanceName}" : null,
            Pagination = new PaginationState { TotalItems = savedConnections.Count },
        };

        page.AddColumn(ListColumnDefinition.Create(ColumnName, "Name"));
        page.AddColumn(ListColumnDefinition.Create(ColumnUrl, "URL"));

        var lastUsedColumn = ListColumnDefinition.Create(ColumnLastUsed, "Last Used");
        lastUsedColumn.Alignment = ColumnAlignments.Right;
        page.AddColumn(lastUsedColumn);

        foreach (var connection in savedConnections)
        {
            var row = new ListRowModel { Id = connection.Name };
            row.SetValue(ColumnName, connection.Name);
            row.SetValue(ColumnUrl, connection.Url);
            row.SetValue(
                ColumnLastUsed,
                connection.LastUsed?.ToString("g", CultureInfo.CurrentCulture) ?? "Never");

            // Why: flag the live connection with a semantic row status rather than a colour. Each
            // renderer decides how to express it (a highlight in the terminal, a badge on the web),
            // so the screen never encodes presentation.
            if (status.IsConnected
                && string.Equals(status.InstanceName, connection.Name, StringComparison.Ordinal))
            {
                row.Status = RowStatuses.Success;
            }

            page.AddRow(row);
        }

        page.AddListAction(new PageAction { Id = ActionNew, Label = "New Connection", Shortcut = 'n' });

        // Why 'u' and not 'q': the renderer reserves [q] for its own Back choice.
        page.AddListAction(new PageAction { Id = ActionQuickConnect, Label = "Quick Connect (URL)", Shortcut = 'u' });

        if (status.IsConnected)
        {
            page.AddListAction(new PageAction
            {
                Id = ActionDisconnect,
                Label = "Disconnect",
                Shortcut = 'd',
                IsDestructive = true,
            });
        }

        return page;
    }

    private async Task AddNewConnection()
    {
        Console.WriteLine();
        Console.MarkupLine($"[{Theme.Colors.Secondary}]Add New Connection[/]");
        Console.WriteLine();

        var name = Console.Prompt(new TextPrompt<string>($"[{Theme.Colors.Primary}]Connection name:[/]").Culture(CultureInfo.CurrentCulture));
        var url = Console.Prompt(new TextPrompt<string>($"[{Theme.Colors.Primary}]Instance URL:[/]").Culture(CultureInfo.CurrentCulture));

        var useAuth = Console.Confirm($"[{Theme.Colors.Primary}]Requires authentication?[/]", false);

        string? apiKey = null;
        if (useAuth)
        {
            apiKey = Console.Prompt(
                new TextPrompt<string>($"[{Theme.Colors.Primary}]API Key:[/]")
                    .Secret());
        }

        var connection = new SavedConnection
        {
            Name = name,
            Url = url,
            ApiKey = apiKey
        };

        _connectionManager.SaveConnection(connection);
        RenderSuccess($"Connection '{name}' saved.");

        // Ask if they want to connect now
        var connectNow = Console.Confirm($"[{Theme.Colors.Primary}]Connect now?[/]", true);
        if (connectNow)
        {
            await ConnectTo(connection).ConfigureAwait(false);
        }
    }

    private Task QuickConnect()
    {
        Console.WriteLine();
        var url = Console.Prompt(new TextPrompt<string>($"[{Theme.Colors.Primary}]Instance URL:[/]").Culture(CultureInfo.CurrentCulture));

        var connection = new SavedConnection
        {
            Name = "Quick Connect",
            Url = url
        };

        return ConnectTo(connection);
    }

    private async Task ConnectTo(SavedConnection connection)
    {
        await Console.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(new Style(Theme.Colors.Primary))
            .StartAsync($"Connecting to {connection.Url}...", async ctx =>
            {
                var result = await _connectionManager.Connect(connection).ConfigureAwait(false);

                if (result.Success)
                {
                    ctx.Status($"[{Theme.Colors.Success}]Connected![/]");
                }
                else
                {
                    ctx.Status($"[{Theme.Colors.Error}]Failed: {result.ErrorMessage}[/]");
                }
            }).ConfigureAwait(false);

        await Task.Delay(1500).ConfigureAwait(false);
    }
}
