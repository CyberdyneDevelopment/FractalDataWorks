using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// What is wrong with the text of the settings that FDW's own component kinds held as declared settings until FDW-7275 (a decision's, a loop's test, an Execute pipeline's, a Send's
/// and an Ask's) and now hold in columns of their own: text that names a variable, in a setting that is not read when the step runs. The check
/// <see cref="DeclaredSettingProblems"/> made for them as declared settings, kept for them as columns, so moving them did not loosen what a save refuses.
/// </summary>
public static class OwnTextSettingProblems
{
    private static readonly HashSet<string> Names = new(
        [
            "testOutput", "logic", "outcomes", "targetVariable", "targets", "outputs", "subject", "payloadType",
            "policy", "call", "kind", "continuation", "answerParameter", "title", "body", "options", "grantVariable",
        ],
        StringComparer.OrdinalIgnoreCase);

    /// <summary>Lists the problems of the text of a component's moved settings.</summary>
    /// <param name="component">The stored component.</param>
    /// <param name="kind">The component type it is judged against.</param>
    /// <returns>One problem for each setting that names a variable and is not read when the step runs; empty when none does.</returns>
    public static IEnumerable<GraphProblem> Of(PipelineComponentConfiguration component, IComponentType kind)
    {
        foreach (var setting in kind.Settings.Where(member => Names.Contains(member.Name) && !member.ReadsWhenStepRuns))
        {
            if (ComponentSettingBindings.Find(setting.Name)?.Get(component) is string text && VariableText.Names(text).Count > 0)
            {
                yield return new GraphProblem(
                    ["components", component.Name, "settings", setting.Name],
                    $"component '{component.Name}' states '{text}' for its setting '{setting.Name}', which names a variable; only a setting read when the step runs may name one");
            }
        }
    }
}
