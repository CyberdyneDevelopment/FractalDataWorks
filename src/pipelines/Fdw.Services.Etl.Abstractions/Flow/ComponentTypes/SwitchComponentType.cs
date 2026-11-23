using System;
using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Takes the value of a variable and fires the one output that names it; a value no output names fires <c>otherwise</c>, or fails the run when the switch says so (FDW-6134).</summary>
[TypeOption(typeof(ComponentTypes), "Switch")]
public sealed class SwitchComponentType : ComponentTypeBase, IDecisionComponentType
{
    /// <summary>The name of the setting that lists the outcomes.</summary>
    public const string OutcomesSetting = "outcomes";

    /// <summary>The name of the setting that says whether a value no outcome names fires <see cref="Otherwise"/>.</summary>
    public const string OtherwiseSetting = "otherwise";

    /// <summary>The output that fires for a value no outcome names, when the switch has one.</summary>
    public const string Otherwise = "otherwise";

    /// <summary>The characters that separate the outcomes in the <c>outcomes</c> setting.</summary>
    public const char OutcomeSeparator = ',';

    /// <summary>Initializes a new instance of the <see cref="SwitchComponentType"/> class.</summary>
    public SwitchComponentType() : base(
        "Switch",
        "Switch",
        "Takes the value of a variable and fires the one output that names it; the components its lines lead to run only when the output they come from fired.",
        "Control flow",
        false,
        [
            new StageMember("variable", "Variable", true, "The variable whose value picks the output; compared with the outcomes without regard to case."),
            new StageMember(OutcomesSetting, "Text", true, "The outcomes, separated by commas; each is the name of an output."),
            new StageMember(OtherwiseSetting, "Boolean", true, "True: a value no outcome names fires the output otherwise. False: it fails the run."),
        ])
    {
    }

    /// <summary>Reads the outcomes a switch states.</summary>
    /// <param name="text">The text of the <c>outcomes</c> setting.</param>
    /// <returns>The outcomes, trimmed, without the empty ones.</returns>
    public static IReadOnlyList<string> OutcomesOf(string text)
    {
        // Why by hand: this package targets netstandard2.0, whose Split neither trims nor takes a single character with options.
        List<string> outcomes = [];
        foreach (var part in text.Split(OutcomeSeparator))
        {
            var trimmed = part.Trim();
            if (trimmed.Length > 0)
            {
                outcomes.Add(trimmed);
            }
        }

        return outcomes;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> FiredOutputs(Func<string, string?> stated)
    {
        if (stated(OutcomesSetting) is not { } outcomes)
        {
            return [];
        }

        List<string> fired = [.. OutcomesOf(outcomes)];
        if (stated(OtherwiseSetting) is { } otherwise && string.Equals(otherwise, bool.TrueString, StringComparison.OrdinalIgnoreCase))
        {
            fired.Add(Otherwise);
        }

        return fired;
    }
}
