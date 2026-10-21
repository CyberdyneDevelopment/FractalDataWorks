using System.Collections.Generic;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>One component of a graph as the API states it (FDW-1396, FDW-2041).</summary>
public class GraphComponentPayload
{
    /// <summary>Gets or sets the component's name, unique within the graph.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the component's kind, a <c>ComponentTypes</c> option name.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the loop component whose body this component is in; null for a top-level component.</summary>
    public string? Parent { get; set; }

    /// <summary>
    /// Gets or sets the settings the kind takes, by the names the catalogue gives them. A text, number or boolean setting is that
    /// JSON value; a <c>ColumnList</c> is an array of column names; <c>FieldMaps</c> is an array of
    /// <see cref="GraphFieldMapPayload"/>, or for a component's <c>mappings</c> also <c>{ "parameter": "Name" }</c>, a list run parameter of <c>Source -&gt; Target</c> items; <c>SplitOutputs</c> is an array of <see cref="GraphSplitOutputPayload"/>; <c>ValueSources</c> is an array of <c>PipelineLoadColumnSource</c>, the shape a load step states them in; <c>Parameters</c> is an array of <see cref="GraphComponentParameterPayload"/>. On a
    /// request each value arrives as the JSON the caller sent.
    /// </summary>
    public IDictionary<string, object?> Settings { get; set; } = new Dictionary<string, object?>(System.StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets or sets where the console draws the component; the engine never reads it.</summary>
    public GraphLayoutPayload? Layout { get; set; }
}
