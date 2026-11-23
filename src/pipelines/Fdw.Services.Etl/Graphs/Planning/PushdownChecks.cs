using System.Collections.Generic;
using System.Linq;
using Fdw.Data;
using Fdw.Services.Etl.Abstractions.OptionTypes;

namespace Fdw.Services.Etl.Graphs.Planning;

/// <summary>
/// The settings that keep a component out of a pushed query (FDW-6969): each is something one query in the store does not do, so the component
/// runs in memory. Each check answers the reason in words for the trace, or empty when the component may be pushed.
/// </summary>
public static class PushdownChecks
{
    /// <summary>Checks what every pushed component must not state: a loop, a stage, a completion line, an error output, stages or bound settings.</summary>
    /// <param name="scope">The graph.</param>
    /// <param name="component">The component.</param>
    /// <returns>Why it runs in memory, or empty.</returns>
    public static string Plain(PushdownScope scope, PipelineComponentConfiguration component)
    {
        if (component.ParentComponent is not null)
            return "it runs inside a loop";
        if (component.Stage is not null)
            return "it runs in a stage of its own";
        if (scope.Completes(component.Name))
            return "a completion line leaves or waits on it";
        if (component.ErrorOutput is not null)
            return "it sends the rows it refuses to an error output";
        if (component.Stages.Count > 0)
            return "it applies stages of its own to its rows";
        return component.SettingBindings.Count > 0 || component.ValueSources.Count > 0 || component.Parameters.Count > 0
            ? "a setting of it takes a value the run supplies"
            : string.Empty;
    }

    /// <summary>Checks what a container source must not state: a run value in its address, another physical address, a list of files.</summary>
    /// <param name="component">The source.</param>
    /// <returns>Why it is read in memory, or empty.</returns>
    public static string Source(PipelineComponentConfiguration component)
    {
        if (string.IsNullOrWhiteSpace(component.DataStoreName) || string.IsNullOrWhiteSpace(component.ContainerName))
            return "it does not state its data store and container";
        if (new[] { component.DataStoreName, component.PathName, component.ContainerName }.Any(ParameterSettings.HoldsParameter))
            return "its address names a value the run supplies";
        if (component.TargetKind is { } kind && ReferenceEquals(TargetKinds.ByName(kind), TargetKinds.Files))
            return "it reads a list of files";
        return !string.IsNullOrWhiteSpace(component.PhysicalPath) || !string.IsNullOrWhiteSpace(component.PhysicalName)
            ? "it reads its container at another physical path or name"
            : string.Empty;
    }

    /// <summary>Finds the first name an SQL statement would not write as it is.</summary>
    /// <param name="names">The names.</param>
    /// <returns>The first such name, or null when every one is plain.</returns>
    public static string? FirstNotPlain(IEnumerable<string> names) => names.FirstOrDefault(name => !PlainIdentifiers.IsPlain(name));
}
