using System;
using System.Collections.Generic;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Workspaces.Charts;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Reads the chart a data view may hold: a chart type with its encoding, or neither (FDW-1032, FDW-2365).</summary>
internal static class DataViewChart
{
    /// <summary>Reads what a request sent for one half of the chart.</summary>
    /// <param name="sent">What the request carried.</param>
    /// <returns>Null when the request sent nothing or only white space, else what it sent.</returns>
    internal static string? Given(string? sent) => string.IsNullOrWhiteSpace(sent) ? null : sent;

    /// <summary>Says why a chart cannot be drawn, or that there is no chart to draw.</summary>
    /// <param name="chartType">The chart type the view will hold.</param>
    /// <param name="encoding">The encoding the view will hold.</param>
    /// <param name="dataSetFields">The fields of the data set the view reads.</param>
    /// <returns>Null when the view has no chart or its chart can be drawn; else the reason it cannot.</returns>
    internal static string? Reason(string? chartType, string? encoding, IEnumerable<DataSetFieldConfiguration> dataSetFields)
    {
        var type = Given(chartType);
        var plotted = Given(encoding);

        // Why a chart is two parts: a type says what is drawn and an encoding says from which fields, so either alone draws nothing.
        if (type is null && plotted is null)
        {
            return null;
        }

        if (type is null)
        {
            return "a chart needs a chartType as well as an encoding, and chartType is missing";
        }

        return plotted is null
            ? "a chart needs an encoding as well as a chartType, and encoding is missing"
            : ChartSpecificationCheck.Reason(type, plotted, dataSetFields);
    }
}
