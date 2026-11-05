using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// Works out what a run's flow moved from its report (FDW-3381): the figures of each component, and the totals of the flow.
/// </summary>
/// <remarks>
/// The report of one stage of a run holds the lines that leave its components and, in <see cref="FlowRunReport.Arriving"/>, the lines that arrive from other stages; a
/// component's rows in and the flow's sources are worked out from both. A line whose target names are empty is a dropped output: its rows were discarded, so they count as rows the component sent on but not as rows that arrived
/// anywhere and not as bytes moved. Rows read are what components with no line into them sent on (the sources); rows written are what arrived at components with no line out
/// of them (the destinations).
/// </remarks>
internal static class FlowFigures
{
    /// <summary>Works out what one component moved.</summary>
    /// <param name="report">The flow's report.</param>
    /// <param name="component">The component's name.</param>
    /// <returns>Its figures.</returns>
    public static ComponentFigures Of(FlowRunReport report, string component)
    {
        var from = report.Links.Where(link => Named(link.Link.FromComponent, component)).ToList();
        return new ComponentFigures(
            Into(report).Where(link => Real(link) && Named(link.Link.ToComponent, component)).Sum(link => link.Rows),
            from.Sum(link => link.Rows),
            from.Where(Real).Sum(link => link.Bytes),
            (report.Rejections ?? []).Where(rejection => Named(rejection.Component, component)).Sum(rejection => rejection.Count));
    }

    /// <summary>Works out the totals of the flow.</summary>
    /// <param name="report">The flow's report.</param>
    /// <returns>The totals, by the name they are recorded under.</returns>
    public static IReadOnlyDictionary<string, object?> Totals(FlowRunReport report)
    {
        var sources = report.Components.Where(component => !Into(report).Any(link => Real(link) && Named(link.Link.ToComponent, component.Name))).ToList();
        var destinations = report.Components.Where(component => !report.Links.Any(link => Named(link.Link.FromComponent, component.Name))).ToList();
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["rowsRead"] = sources.Sum(component => Of(report, component.Name).RowsOut),
            ["rowsWritten"] = destinations.Sum(component => Of(report, component.Name).RowsIn),
            ["rowsRejected"] = (report.Rejections ?? []).Sum(rejection => rejection.Count),
            ["bytesMoved"] = report.Links.Where(Real).Sum(link => link.Bytes),
        };
    }

    // Every line that arrives at the report's components: the report's own lines and those that arrive from other stages.
    private static IEnumerable<FlowLinkCount> Into(FlowRunReport report) => report.Links.Concat(report.Arriving ?? []);

    private static bool Real(FlowLinkCount link) => !string.IsNullOrEmpty(link.Link.ToComponent);

    private static bool Named(string left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
