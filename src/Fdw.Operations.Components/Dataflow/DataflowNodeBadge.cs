using Fdw.UI.Components.Services;

namespace Fdw.Operations.Components.Dataflow;

/// <summary>
/// Chooses the badge tone a dataflow node is drawn in.
/// </summary>
/// <remarks>
/// A node kind is not a status, so it is not a member of <see cref="StatusVariants"/> — but the graph
/// legend is drawn in the same five tones, and the page that draws it should not be the place that
/// decides which. The tones separate what moves data (pipeline, dataset) from what holds it
/// (datastore) and what reaches out of the system (connection).
/// </remarks>
public static class DataflowNodeBadge
{
    /// <summary>
    /// Gets the tone for a dataflow node kind.
    /// </summary>
    /// <param name="nodeType">The node kind name.</param>
    /// <returns>The variant the pill is drawn in.</returns>
    public static StatusVariantBase Variant(string nodeType) =>
        nodeType switch
        {
            "Pipeline" or "DataSet" => StatusVariants.Info,
            "DataStore" => StatusVariants.Success,
            "Connection" => StatusVariants.Warning,
            _ => StatusVariants.Neutral,
        };
}
