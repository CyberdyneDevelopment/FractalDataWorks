using System;
using System.Threading.Tasks;
using Fdw.TUI.Management.Navigation;
using Fdw.UI.Themes;
using Spectre.Console;

namespace Fdw.TUI.Management.Screens;

/// <summary>
/// Base class for screens providing common functionality.
/// </summary>
public abstract class ScreenBase : IScreen
{
    /// <summary>
    /// Gets the console instance.
    /// </summary>
    protected IAnsiConsole Console { get; }

    /// <summary>
    /// Gets the theme.
    /// </summary>
    protected IMenuTheme Theme { get; }

    /// <summary>
    /// Gets the screen factory.
    /// </summary>
    protected IScreenFactory ScreenFactory { get; }

    /// <inheritdoc />
    public abstract string Title { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenBase"/> class.
    /// </summary>
    protected ScreenBase(IAnsiConsole console, IMenuTheme theme, IScreenFactory screenFactory)
    {
        Console = console ?? throw new ArgumentNullException(nameof(console));
        Theme = theme ?? throw new ArgumentNullException(nameof(theme));
        ScreenFactory = screenFactory ?? throw new ArgumentNullException(nameof(screenFactory));
    }

    /// <inheritdoc />
    public abstract Task<NavigationResult> Show();

    /// <summary>
    /// Clears the console and renders the screen header.
    /// </summary>
    protected void RenderHeader()
    {
        Console.Clear();
        var rule = new Rule($"[{Theme.Colors.Primary} bold]{Title}[/]")
        {
            Justification = Justify.Left,
            Style = new Style(Theme.Colors.Primary)
        };
        Console.Write(rule);
        Console.WriteLine();
    }

    /// <summary>
    /// Renders a status message.
    /// </summary>
    protected void RenderStatus(string message, bool isError = false)
    {
        var color = isError ? Theme.Colors.Error : Theme.Colors.Info;
        var icon = isError ? Theme.Icons.ErrorIcon : Theme.Icons.InfoIcon;
        Console.MarkupLine($"[{color}]{icon} {Markup.Escape(message)}[/]");
    }

    /// <summary>
    /// Renders a success message.
    /// </summary>
    protected void RenderSuccess(string message)
    {
        Console.MarkupLine($"[{Theme.Colors.Success}]{Theme.Icons.SuccessIcon} {Markup.Escape(message)}[/]");
    }

    /// <summary>
    /// Renders a warning message.
    /// </summary>
    protected void RenderWarning(string message)
    {
        Console.MarkupLine($"[{Theme.Colors.Warning}]{Theme.Icons.WarningIcon} {Markup.Escape(message)}[/]");
    }
}
