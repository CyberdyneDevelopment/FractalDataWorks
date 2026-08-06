using Spectre.Console;

namespace Fdw.UI.Components.TUI.Prompts;

/// <summary>
/// Helper for creating Yes/No confirmation prompts.
/// </summary>
public static class ConfirmationPromptHelper
{
    /// <summary>
    /// Prompts the user for a Yes/No confirmation.
    /// </summary>
    /// <param name="console">The console to prompt on</param>
    /// <param name="promptText">The prompt text to display</param>
    /// <param name="defaultValue">The default value</param>
    /// <param name="theme">Theme configuration</param>
    /// <returns>True if confirmed, false otherwise</returns>
    public static bool Prompt(
        IAnsiConsole console,
        string promptText,
        bool defaultValue = false,
        TUIThemeConfiguration? theme = null)
    {
        return console.Confirm(promptText, defaultValue);
    }
}
