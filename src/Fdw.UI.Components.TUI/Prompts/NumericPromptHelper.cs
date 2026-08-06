using System;
using Spectre.Console;
using Fdw.UI.Abstractions;

namespace Fdw.UI.Components.TUI.Prompts;

/// <summary>
/// Helper for creating numeric prompts with range validation.
/// </summary>
public static class NumericPromptHelper
{
    /// <summary>
    /// Prompts the user to enter a numeric value with range validation.
    /// </summary>
    /// <typeparam name="T">The numeric type</typeparam>
    /// <param name="console">The console to prompt on</param>
    /// <param name="promptText">The prompt text to display</param>
    /// <param name="defaultValue">The default value</param>
    /// <param name="metadata">Property metadata for validation</param>
    /// <param name="theme">Theme configuration</param>
    /// <returns>The entered numeric value</returns>
    public static T Prompt<T>(
        IAnsiConsole console,
        string promptText,
        T? defaultValue = default,
        PropertyMetadata? metadata = null,
        TUIThemeConfiguration? theme = null)
        where T : struct, IComparable<T>
    {
        var prompt = new TextPrompt<T>(promptText);

        if (defaultValue != null && !defaultValue.Equals(default(T)))
        {
            prompt.DefaultValue(defaultValue.Value);
        }

        // Range validation
        if (metadata?.ValidationRules.TryGetValue("min", out var minObj) == true)
        {
            var min = (T)Convert.ChangeType(minObj, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
            prompt.Validate(value =>
            {
                if (value.CompareTo(min) < 0)
                {
                    return ValidationResult.Error($"[{theme?.Colors.Error ?? Color.Red}]Value must be at least {min}[/]");
                }
                return ValidationResult.Success();
            });
        }

        if (metadata?.ValidationRules.TryGetValue("max", out var maxObj) == true)
        {
            var max = (T)Convert.ChangeType(maxObj, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
            prompt.Validate(value =>
            {
                if (value.CompareTo(max) > 0)
                {
                    return ValidationResult.Error($"[{theme?.Colors.Error ?? Color.Red}]Value must be at most {max}[/]");
                }
                return ValidationResult.Success();
            });
        }

        return console.Prompt(prompt);
    }
}
