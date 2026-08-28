namespace Fdw.UI.Abstractions.Charts;

/// <summary>
/// Binds a single data field to a chart encoding role.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="ChartEncoding"/> pairs a role (e.g. <c>X</c>, <c>Color</c>) with the name of the
/// data field that fills it (e.g. <c>"OrderDate"</c>). The optional
/// <see cref="AggregationHint"/> lets the model carry a display-level hint to the renderer without
/// coupling to a specific aggregation engine — the chart layer does not compute aggregations.
/// </para>
/// <para>
/// <see cref="IChartModel.Encodings"/> holds one entry per bound role. Unbound roles are absent
/// from the collection — no null entries, no sentinel fields.
/// </para>
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ChartEncoding
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChartEncoding"/> class.
    /// </summary>
    /// <param name="role">The encoding role this binding satisfies.</param>
    /// <param name="fieldName">The name of the data field bound to this role.</param>
    /// <param name="aggregationHint">
    /// An optional aggregation hint for the renderer (e.g. <c>"Sum"</c>, <c>"Count"</c>).
    /// Null means no hint — the renderer uses its default behaviour.
    /// </param>
    public ChartEncoding(IChartEncodingRole role, string fieldName, string? aggregationHint = null)
    {
        Role = role;
        FieldName = fieldName;
        AggregationHint = aggregationHint;
    }

    /// <summary>
    /// Gets the encoding role this binding satisfies (e.g. X, Y, Color).
    /// </summary>
    public IChartEncodingRole Role { get; }

    /// <summary>
    /// Gets the name of the data field bound to this role.
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// Gets an optional aggregation hint passed to the renderer (e.g. "Sum", "Count", "Avg").
    /// </summary>
    /// <remarks>
    /// Null means no hint. The chart contract layer does not interpret this value — it is a
    /// convention between the domain provider that builds the <see cref="IChartModel"/> and the
    /// renderer.
    /// </remarks>
    public string? AggregationHint { get; }
}
