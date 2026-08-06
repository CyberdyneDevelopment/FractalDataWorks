using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Spectre.Console;
using Fdw.UI.Abstractions;
using Fdw.Collections;

namespace Fdw.UI.Components.TUI;

/// <summary>
/// CRTP base for Terminal UI components using Spectre.Console.
/// Provides interactive prompting and rich console rendering.
/// </summary>
/// <typeparam name="TSelf">The derived TUI component type (CRTP)</typeparam>
/// <typeparam name="TModel">The model type being rendered</typeparam>
public abstract class TUIComponent<TSelf, TModel> : ComponentBase<TSelf, TModel>
    where TSelf : TUIComponent<TSelf, TModel>
{
    /// <summary>
    /// Theme configuration for this component.
    /// </summary>
    public TUIThemeConfiguration? Theme { get; set; }

    /// <summary>
    /// Prompts the user to enter/edit the model value interactively.
    /// </summary>
    /// <param name="console">The console to prompt on</param>
    /// <returns>The entered/edited model value</returns>
    public abstract Task<TModel?> Prompt(IAnsiConsole console);

    /// <summary>
    /// Renders the current model value to the console (read-only display).
    /// </summary>
    /// <param name="console">The console to render to</param>
    public abstract void Render(IAnsiConsole console);

    /// <summary>
    /// Gets display text for this component (for use in lists/tables).
    /// </summary>
    public virtual string GetDisplayText()
    {
        return Value?.ToString() ?? "[dim]null[/]";
    }

    /// <summary>
    /// Renders a section header with the component name.
    /// </summary>
    protected virtual void RenderHeader(IAnsiConsole console, string? title = null)
    {
        var headerTitle = title ?? typeof(TModel).Name;
        var color = Theme?.Colors.Primary ?? Color.Yellow;

        console.Write(new Rule($"[{color}]{headerTitle}[/]").LeftJustified());
    }

    /// <summary>
    /// Prompts for a TypeCollection selection.
    /// Callers pass <c>MyTypes.All()</c> to avoid reflection.
    /// </summary>
    protected virtual int PromptTypeCollectionId<TOption>(
        IAnsiConsole console,
        string promptText,
        IEnumerable<TOption> options)
        where TOption : class, ITypeOption
    {
        return Prompts.TypeCollectionPromptHelper.Prompt(
            console,
            promptText,
            options,
            Theme);
    }
}
