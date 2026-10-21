using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// Text that names variables in double braces (FDW-1403): <c>{{Attempt}}</c> in a parameter value is the value of the variable
/// <c>Attempt</c> when the step runs.
/// </summary>
public static partial class VariableText
{
    [GeneratedRegex(@"\{\{\s*(?<name>[^{}\s]+)\s*\}\}", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Reference();

    [GeneratedRegex(@"^\s*\{\{\s*(?<name>[^{}\s]+)\s*\}\}\s*$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Whole();

    /// <summary>
    /// Reads a text as a value: when the whole text is one reference, the value of that variable as it is (a null stays null, a
    /// number stays a number); otherwise the text with each reference replaced, as <see cref="Render"/> does.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>What makes the value from the variables in scope when the step runs.</returns>
    public static Func<FlowVariables, object?> Typed(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Whole().Match(text) is { Success: true } whole
            ? variables => variables.TryGet(whole.Groups["name"].Value, out var value)
                ? value
                : throw Missing(whole.Groups["name"].Value, variables)
            : variables => Render(text, variables);
    }

    /// <summary>Lists the variables a text names, in the order it names them.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The names; one for each reference, a name repeated as often as the text repeats it.</returns>
    public static IReadOnlyList<string> Names(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var names = new List<string>();
        foreach (Match match in Reference().Matches(text))
        {
            names.Add(match.Groups["name"].Value);
        }

        return names;
    }

    /// <summary>Replaces each reference with the value of the variable it names.</summary>
    /// <param name="text">The text.</param>
    /// <param name="variables">The variables in scope.</param>
    /// <returns>The text with every reference replaced; a null value is the empty text.</returns>
    /// <exception cref="InvalidOperationException">A reference names a variable that is not declared in scope; the graph check refuses such a graph before it runs.</exception>
    public static string Render(string text, FlowVariables variables)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(variables);
        return Reference().Replace(text, match =>
            variables.TryGet(match.Groups["name"].Value, out var value)
                ? TextOf(value)
                : throw Missing(match.Groups["name"].Value, variables));
    }

    // Why two messages: a parameter the run left blank is declared and has no value, which is not the same mistake as a misspelt name.
    private static InvalidOperationException Missing(string name, FlowVariables variables) =>
        variables.IsBlank(name)
            ? new InvalidOperationException($"The text names the parameter '{name}', which this run was started without a value for.")
            : new InvalidOperationException($"The text names the variable '{name}', which is not declared in scope.");

    // A null value renders as no text; every other value renders in the invariant culture so the text does not change with the host.
    private static string TextOf(object? value) =>
        value is not null && Convert.ToString(value, CultureInfo.InvariantCulture) is { } text ? text : string.Empty;
}
