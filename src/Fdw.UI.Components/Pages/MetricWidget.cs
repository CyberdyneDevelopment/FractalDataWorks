using Fdw.UI.Abstractions.Pages;

namespace Fdw.UI.Components.Pages;

/// <summary>
/// Concrete implementation of a metric widget.
/// </summary>
public sealed class MetricWidget : IMetricWidget
{
    /// <inheritdoc />
    public string Id { get; set; } = "";

    /// <inheritdoc />
    public string Label { get; set; } = "";

    /// <inheritdoc />
    public decimal Value { get; set; }

    /// <inheritdoc />
    public string? FormatString { get; set; }

    /// <inheritdoc />
    public string? Unit { get; set; }

    /// <inheritdoc />
    public ITrendDirection Trend { get; set; } = TrendDirections.None;

    /// <inheritdoc />
    public decimal? TrendPercentage { get; set; }

    /// <inheritdoc />
    public string? Icon { get; set; }

    /// <inheritdoc />
    public string? NavigationTarget { get; set; }

    /// <summary>
    /// Creates a count metric.
    /// </summary>
    public static MetricWidget Count(string id, string label, int count) =>
        new() { Id = id, Label = label, Value = count, FormatString = "N0" };

    /// <summary>
    /// Creates a percentage metric.
    /// </summary>
    public static MetricWidget Percentage(string id, string label, decimal percentage) =>
        new() { Id = id, Label = label, Value = percentage, FormatString = "P1" };

    /// <summary>
    /// Creates a duration metric in milliseconds.
    /// </summary>
    public static MetricWidget Duration(string id, string label, decimal milliseconds) =>
        new() { Id = id, Label = label, Value = milliseconds, FormatString = "N0", Unit = "ms" };
}