using Spectre.Console;

namespace Fdw.UI.Components.TUI.Renderers;

/// <summary>
/// Renders content in bordered panels.
/// </summary>
public static class PanelRenderer
{
    /// <summary>
    /// Renders content in a bordered panel.
    /// </summary>
    /// <param name="console">The console to render to</param>
    /// <param name="content">The content to render</param>
    /// <param name="title">Optional panel title</param>
    /// <param name="theme">Theme configuration</param>
    public static void RenderPanel(
        IAnsiConsole console,
        string content,
        string? title = null,
        TUIThemeConfiguration? theme = null)
    {
        var border = theme?.Borders.Panel ?? BoxBorder.Rounded;
        var borderColor = theme?.Colors.Primary ?? Color.Blue;

        var panel = new Panel(new Markup(content))
            .Border(border)
            .BorderColor(borderColor);

        if (!string.IsNullOrEmpty(title))
        {
            panel.Header(title);
        }

        console.Write(panel);
    }
}
