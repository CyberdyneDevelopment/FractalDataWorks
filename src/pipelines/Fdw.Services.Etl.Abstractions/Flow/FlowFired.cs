using System;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// What a decision fired, kept in the variables of the level it ran in (FDW-6134): the boolean variable <c>&lt;component&gt;.&lt;output&gt;</c> for every output the decision has,
/// written each time it runs. One fact serves the components its lines feed, a gate that reads its lines as values and a loop that takes an output as its test.
/// </summary>
public static class FlowFired
{
    /// <summary>Names the variable that holds whether an output of a decision fired.</summary>
    /// <param name="component">The decision.</param>
    /// <param name="output">Its output.</param>
    /// <returns>The variable's name.</returns>
    public static string VariableOf(string component, string output) => string.Concat(component, ".", output);

    /// <summary>Records whether an output of a decision fired.</summary>
    /// <param name="variables">The variables of the level the decision ran in.</param>
    /// <param name="component">The decision.</param>
    /// <param name="output">Its output.</param>
    /// <param name="fired">Whether the output fired.</param>
    public static void Set(FlowVariables variables, string component, string output, bool fired)
    {
        var name = VariableOf(component, output);
        if (!variables.TrySet(name, fired))
        {
            variables.Declare(name, fired);
        }
    }

    /// <summary>Says whether an output of a decision fired. A decision that has not run, or was skipped, has fired nothing.</summary>
    /// <param name="variables">The variables in scope.</param>
    /// <param name="signal">The output.</param>
    /// <returns><see langword="true"/> when the output fired.</returns>
    public static bool Has(FlowVariables variables, FlowSignal signal) =>
        variables.TryGet(VariableOf(signal.Component, signal.Output), out var value) && value is true;

    /// <summary>Splits a loop's test setting, <c>component.output</c>, into the output it names.</summary>
    /// <param name="text">The setting's text.</param>
    /// <returns>The output, or <see langword="null"/> when the text is not a component, a dot and an output.</returns>
    public static FlowSignal? Parse(string text)
    {
        var dot = text.LastIndexOf('.');
        return dot < 1 || dot == text.Length - 1 ? null : new FlowSignal(text.Substring(0, dot).Trim(), text.Substring(dot + 1).Trim());
    }
}
