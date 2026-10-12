using System.Collections.Generic;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Describes a windowed (analytic) calculation, including partition, order, and the window function to apply.
/// </summary>
public sealed class WindowedCalculationSpec
{
    /// <summary>Gets the fields used to partition rows for the window.</summary>
    public IReadOnlyList<string> PartitionByFields { get; init; } = [];

    /// <summary>Gets the fields and directions used to order rows within each partition.</summary>
    public IReadOnlyList<WindowOrderField> OrderByFields { get; init; } = [];

    /// <summary>Gets the source field the window function operates on.</summary>
    public string TargetField { get; init; } = string.Empty;

    /// <summary>Gets the name of the window function to apply (e.g. "ROW_NUMBER", "SUM").</summary>
    public string WindowFunction { get; init; } = string.Empty;

    /// <summary>Gets the name of the output field that receives the window function result.</summary>
    public string OutputFieldName { get; init; } = string.Empty;
}
