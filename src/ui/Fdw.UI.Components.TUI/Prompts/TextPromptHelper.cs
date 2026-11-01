using System;
using Fdw.Conventions;
using Fdw.UI.Abstractions;
using Spectre.Console;

namespace Fdw.UI.Components.TUI.Prompts;

/// <summary>
/// Helper for creating text prompts with validation.
/// </summary>
public static class TextPromptHelper
{
    /// <summary>
    /// Prompts the user to enter a text value with validation.
    /// </summary>
    /// <param name="console">The console to prompt on</param>
    /// <param name="promptText">The prompt text to display</param>
    /// <param name="defaultValue">The default value</param>
    /// <param name="metadata">Property metadata for validation</param>
    /// <param name="theme">Theme configuration</param>
    /// <returns>The entered text value</returns>
    [ConventionOverride(MaxCyclomaticComplexity = 15)]  // TUI validation logic — independent validation rule checks
    public static string Prompt(
        IAnsiConsole console,
        string promptText,
        string? defaultValue = null,
        PropertyMetadata? metadata = null,
        TUIThemeConfiguration? theme = null)
    {
        var prompt = new TextPrompt<string>(promptText);

        if (!string.IsNullOrEmpty(defaultValue))
        {
            prompt.DefaultValue(defaultValue);
        }

        if (metadata?.Required == true)
        {
            prompt.AllowEmpty = false;
            prompt.ValidationErrorMessage($"[{theme?.Colors.Error ?? Color.Red}]{promptText} is required[/]");
        }

        if (metadata?.ValidationRules.TryGetValue("maxLength", out var maxLengthObj) == true
            && maxLengthObj is int maxLength)
        {
            prompt.Validate(value =>
            {
                if (value.Length > maxLength)
                {
                    return ValidationResult.Error($"[{theme?.Colors.Error ?? Color.Red}]Maximum length is {maxLength}[/]");
                }
                return ValidationResult.Success();
            });
        }

        if (metadata?.ValidationRules.TryGetValue("pattern", out var patternObj) == true
            && patternObj is string pattern)
        {
            var regex = new System.Text.RegularExpressions.Regex(
                pattern,
                System.Text.RegularExpressions.RegexOptions.None,
                TimeSpan.FromSeconds(1));
            prompt.Validate(value =>
            {
                if (!regex.IsMatch(value))
                {
                    return ValidationResult.Error($"[{theme?.Colors.Error ?? Color.Red}]Invalid format[/]");
                }
                return ValidationResult.Success();
            });
        }

        return console.Prompt(prompt);
    }
}
