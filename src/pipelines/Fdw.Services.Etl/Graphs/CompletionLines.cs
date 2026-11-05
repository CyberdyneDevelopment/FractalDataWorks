using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// Tells the lines of a stored graph apart (FDW-3640): a line that leaves a component's <c>done</c> output is a completion line and carries no rows; every other
/// line is a row line. Every reader of the stored lines that means rows reads them through here, so none mistakes a completion line for a row node.
/// </summary>
public static class CompletionLines
{
    /// <summary>Says whether a stored line is a completion line.</summary>
    /// <param name="link">The stored line.</param>
    /// <returns><see langword="true"/> when it leaves a <c>done</c> output.</returns>
    public static bool Is(PipelineLinkConfiguration link)
    {
        ArgumentNullException.ThrowIfNull(link);
        return CompletionNodes.IsDone(link.FromOutput);
    }

    /// <summary>Lists the lines that carry rows.</summary>
    /// <param name="links">The stored lines.</param>
    /// <returns>The row lines, in order.</returns>
    public static IReadOnlyList<PipelineLinkConfiguration> Rows(IEnumerable<PipelineLinkConfiguration> links)
    {
        ArgumentNullException.ThrowIfNull(links);
        return [.. links.Where(link => !Is(link))];
    }

    /// <summary>Lists the completion lines.</summary>
    /// <param name="links">The stored lines.</param>
    /// <returns>The completion lines, in order.</returns>
    public static IReadOnlyList<PipelineLinkConfiguration> Completions(IEnumerable<PipelineLinkConfiguration> links)
    {
        ArgumentNullException.ThrowIfNull(links);
        return [.. links.Where(Is)];
    }

    /// <summary>Makes the flow form of a stored completion line.</summary>
    /// <param name="link">The stored completion line.</param>
    /// <returns>The component that must finish and the one that waits for it.</returns>
    public static FlowCompletion ToCompletion(PipelineLinkConfiguration link)
    {
        ArgumentNullException.ThrowIfNull(link);
        return new FlowCompletion(link.FromComponent, link.ToComponent);
    }
}
