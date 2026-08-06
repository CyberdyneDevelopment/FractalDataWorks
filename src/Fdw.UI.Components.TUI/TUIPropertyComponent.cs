using System;
using Spectre.Console;
using Fdw.UI.Abstractions;

namespace Fdw.UI.Components.TUI;

/// <summary>
/// CRTP base for Terminal UI property-level components.
/// </summary>
/// <typeparam name="TSelf">The derived property component type (CRTP)</typeparam>
/// <typeparam name="TProperty">The property value type</typeparam>
public abstract class TUIPropertyComponent<TSelf, TProperty> : PropertyComponent<TSelf, TProperty>
    where TSelf : TUIPropertyComponent<TSelf, TProperty>
{
    /// <summary>
    /// Theme configuration.
    /// </summary>
    public TUIThemeConfiguration? Theme { get; set; }

    /// <summary>
    /// Prompts the user to enter a value for this property.
    /// </summary>
    public abstract TProperty? PromptValue(IAnsiConsole console);

    /// <summary>
    /// Renders this property value to the console.
    /// </summary>
    public abstract void RenderValue(IAnsiConsole console);

    /// <summary>
    /// Gets the prompt text for this property.
    /// </summary>
    protected virtual string GetPromptText()
    {
        return Metadata?.Label ?? typeof(TProperty).Name;
    }

    /// <summary>
    /// Displays help text if available.
    /// </summary>
    protected virtual void RenderHelpText(IAnsiConsole console)
    {
        if (!string.IsNullOrEmpty(Metadata?.HelpText))
        {
            console.MarkupLine($"[dim]{Metadata.HelpText}[/]");
        }
    }
}
