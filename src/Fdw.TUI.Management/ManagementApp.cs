using System;
using System.Threading.Tasks;
using Fdw.TUI.Management.Navigation;
using Fdw.TUI.Management.Screens;
using Fdw.UI.Themes;
using Spectre.Console;

namespace Fdw.TUI.Management;

/// <summary>
/// Main application class for the TUI Management tool.
/// </summary>
public sealed class ManagementApp
{
    private readonly IAnsiConsole _console;
    private readonly IMenuTheme _theme;
    private readonly INavigationService _navigation;
    private readonly IScreenFactory _screenFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagementApp"/> class.
    /// </summary>
    public ManagementApp(
        IAnsiConsole console,
        IMenuTheme theme,
        INavigationService navigation,
        IScreenFactory screenFactory)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _theme = theme ?? throw new ArgumentNullException(nameof(theme));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _screenFactory = screenFactory ?? throw new ArgumentNullException(nameof(screenFactory));
    }

    /// <summary>
    /// Runs the management application.
    /// </summary>
    /// <returns>Exit code (0 for success).</returns>
    public async Task<int> Run()
    {
        RenderWelcome();

        // Push the main menu onto the navigation stack
        var mainMenu = _screenFactory.Create<MainMenuScreen>();
        _navigation.Push(mainMenu);

        // Main navigation loop
        while (_navigation.HasScreens)
        {
            var currentScreen = _navigation.Current;
            if (currentScreen == null)
            {
                break;
            }

            var result = await currentScreen.Show().ConfigureAwait(false);

            switch (result.Action.Name)
            {
                case "Push":
                    if (result.NextScreen != null)
                    {
                        _navigation.Push(result.NextScreen);
                    }
                    break;

                case "Pop":
                    _navigation.Pop();
                    break;

                case "Replace":
                    if (result.NextScreen != null)
                    {
                        _navigation.Replace(result.NextScreen);
                    }
                    break;

                case "Exit":
                    _navigation.Clear();
                    break;
            }
        }

        RenderGoodbye();
        return 0;
    }

    private void RenderWelcome()
    {
        _console.Clear();
        var panel = new Panel(
            new Markup($"[{_theme.Colors.Muted}]Console-based management for Fdw instances.[/]\n\n" +
                       $"[{_theme.Colors.Info}]Use arrow keys to navigate, Enter to select, and q to go back.[/]"))
        {
            Header = new PanelHeader($"[{_theme.Colors.Primary} bold]Fdw Management Console[/]"),
            Border = BoxBorder.Double,
            BorderStyle = new Style(_theme.Colors.Primary),
            Padding = new Padding(2, 1)
        };

        _console.Write(panel);
        _console.WriteLine();
    }

    private void RenderGoodbye()
    {
        _console.WriteLine();
        _console.MarkupLine($"[{_theme.Colors.Muted}]Thank you for using Fdw Management Console.[/]");
    }
}
