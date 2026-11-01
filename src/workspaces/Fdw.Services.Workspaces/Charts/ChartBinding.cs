namespace Fdw.Services.Workspaces.Charts;

/// <summary>One thing bound to a chart slot: a field as it stands, or an aggregate over a field, or a count of rows.</summary>
/// <param name="Field">The data set field's name; null only for a Count of rows.</param>
/// <param name="Aggregate">The aggregate over the field (<c>Count</c>, <c>Sum</c>, <c>Avg</c>, <c>Min</c>, <c>Max</c>); null when the field is plotted as it stands.</param>
public sealed record ChartBinding(string? Field, string? Aggregate);
