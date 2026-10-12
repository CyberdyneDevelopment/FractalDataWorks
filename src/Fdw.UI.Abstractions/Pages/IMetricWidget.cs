namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// A metric widget showing a numeric value with optional trend.
/// </summary>
public interface IMetricWidget
{
    /// <summary>
    /// Gets the widget identifier.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the metric label.
    /// </summary>
    string Label { get; }

    /// <summary>
    /// Gets the current value.
    /// </summary>
    decimal Value { get; }

    /// <summary>
    /// Gets the value format string (e.g., "N0", "P2").
    /// </summary>
    string? FormatString { get; }

    /// <summary>
    /// Gets the unit suffix (e.g., "ms", "GB", "/sec").
    /// </summary>
    string? Unit { get; }

    /// <summary>
    /// Gets the trend direction compared to previous period.
    /// </summary>
    ITrendDirection Trend { get; }

    /// <summary>
    /// Gets the trend percentage change.
    /// </summary>
    decimal? TrendPercentage { get; }

    /// <summary>
    /// Gets the icon for this widget.
    /// </summary>
    string? Icon { get; }

    /// <summary>
    /// Gets the navigation target when clicked.
    /// </summary>
    string? NavigationTarget { get; }
}