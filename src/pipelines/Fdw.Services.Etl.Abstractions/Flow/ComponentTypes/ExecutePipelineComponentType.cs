using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Starts another pipeline, passing values, and optionally waits for it to end.</summary>
[TypeOption(typeof(ComponentTypes), "ExecutePipeline")]
public sealed class ExecutePipelineComponentType : ComponentTypeBase
{
    /// <summary>The name of the setting that names the variable that holds the pipeline to start (FDW-6135).</summary>
    public const string TargetVariableSetting = "targetVariable";

    /// <summary>The name of the setting that lists the pipelines the component may start through its variable (FDW-6135).</summary>
    public const string TargetsSetting = "targets";

    /// <summary>The name of the setting that maps the started pipeline's outputs to variables (FDW-6136).</summary>
    public const string OutputsSetting = "outputs";

    /// <summary>The characters that separate the pipelines in the <c>targets</c> setting.</summary>
    public const char TargetSeparator = ',';

    /// <summary>Reads the pipelines a component may start through its variable.</summary>
    /// <param name="text">The text of the <c>targets</c> setting.</param>
    /// <returns>The names, trimmed, without the empty ones.</returns>
    public static IReadOnlyList<string> TargetsOf(string text)
    {
        // Why by hand: this package targets netstandard2.0, whose Split does not trim.
        List<string> targets = [];
        foreach (var part in text.Split(TargetSeparator))
        {
            var trimmed = part.Trim();
            if (trimmed.Length > 0)
            {
                targets.Add(trimmed);
            }
        }

        return targets;
    }

    /// <summary>Reads the outputs a component maps to variables: one line for each, an output name, an equals sign and the variable's name.</summary>
    /// <param name="text">The text of the <c>outputs</c> setting.</param>
    /// <param name="map">The pairs of output name and variable name in the order stated; empty when the text is not readable.</param>
    /// <returns>Whether every line is an output name, an equals sign and a variable name; an empty text has no lines, so it reads as no outputs.</returns>
    public static bool TryReadOutputs(string text, out IReadOnlyList<KeyValuePair<string, string>> map)
    {
        // Why by hand: this package targets netstandard2.0, whose Split does not trim.
        List<KeyValuePair<string, string>> pairs = [];
        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            var at = trimmed.IndexOf('=');
            var output = at > 0 ? trimmed.Substring(0, at).Trim() : string.Empty;
            var variable = at > 0 ? trimmed.Substring(at + 1).Trim() : string.Empty;
            if (output.Length == 0 || variable.Length == 0)
            {
                map = [];
                return false;
            }

            pairs.Add(new KeyValuePair<string, string>(output, variable));
        }

        map = pairs;
        return true;
    }

    /// <summary>Initializes a new instance of the <see cref="ExecutePipelineComponentType"/> class.</summary>
    public ExecutePipelineComponentType() : base(
        "ExecutePipeline",
        "Execute pipeline",
        "Starts another pipeline, passing values, and optionally waits for it to end; it can start it when the run fails.",
        "Control flow",
        false,
        [
            new StageMember("pipeline", "Text", false, "The pipeline to start. State this or targetVariable, not both.", SummarisesComponent: true),
            new StageMember(TargetVariableSetting, "Text", false, "The variable that holds the name of the pipeline to start, read when the step runs; the name must be one of targets, and the pipeline one the run's tenant and person are offered. State this or pipeline, not both."),
            new StageMember(TargetsSetting, "Text", false, "The pipelines the component may start through targetVariable, separated by commas; required with targetVariable."),
            new StageMember("parameters", "Parameters", false, "The values of its parameters, as text; a value may name a variable in double braces."),
            new StageMember("awaitCompletion", "Boolean", true, "Whether the component waits for the started pipeline to end."),
            new StageMember("resultVariable", "Variable", false, "A declared variable set to whether the started pipeline succeeded; needs awaitCompletion."),
            new StageMember(OutputsSetting, "Text", false, "The outputs of the started pipeline to read into variables, one line for each as output=variable; needs awaitCompletion. The variable keeps the output's type, so it has to be declared with it."),
            new StageMember("failOnChildFailure", "Boolean", false, "Whether the component fails when the started pipeline does not succeed; needs awaitCompletion."),
            new StageMember("runPoint", "RunPoints", false, "When it starts the pipeline: AfterStage and AfterLoad start it when the run reaches the component (its stage); OnFailure starts it only if the run fails, and only on a component that is not inside a loop. Absent, it starts the pipeline when the run reaches the component. With a run point, the parameter values may use the run's facts: {{PipelineName}}, {{ExecutionId}}, {{StartedAt}}, {{EndedAt}}, {{Outcome}}, {{RowsExtracted}}, {{RowsRejected}} and {{RowsLoaded}}."),
        ])
    {
    }
}
