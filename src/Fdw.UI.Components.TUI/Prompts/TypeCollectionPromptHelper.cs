using System.Collections.Generic;
using System.Linq;
using Spectre.Console;
using Fdw.Collections;

namespace Fdw.UI.Components.TUI.Prompts;

/// <summary>
/// Helper for prompting TypeCollection selections.
/// </summary>
public static class TypeCollectionPromptHelper
{
    /// <summary>
    /// Prompts the user to select from a set of TypeCollection options.
    /// </summary>
    /// <typeparam name="TOption">The TypeOption type</typeparam>
    /// <param name="console">The console to prompt on</param>
    /// <param name="promptText">The prompt text to display</param>
    /// <param name="options">The options to present — callers pass TypeCollection.All()</param>
    /// <param name="theme">Theme configuration</param>
    /// <returns>The selected TypeOption ID, or 0 if no options are available</returns>
    /// <example>
    /// <code>
    /// var id = TypeCollectionPromptHelper.Prompt(console, "Select operator:", FilterOperators.All());
    /// </code>
    /// </example>
    public static int Prompt<TOption>(
        IAnsiConsole console,
        string promptText,
        IEnumerable<TOption> options,
        TUIThemeConfiguration? theme = null)
        where TOption : class, ITypeOption
    {
        var list = options.ToList();

        if (list.Count == 0)
        {
            console.MarkupLine($"[{theme?.Colors.Warning ?? Color.Yellow}]No options available[/]");
            return 0;
        }

        var selection = console.Prompt(
            new SelectionPrompt<TOption>()
                .Title(promptText)
                .AddChoices(list)
                .UseConverter(opt => $"{opt.Name} [dim]({opt.Id})[/]")
                .HighlightStyle(new Style(theme?.Colors.Selected ?? Color.Blue)));

        return (int)selection.Id;
    }
}
