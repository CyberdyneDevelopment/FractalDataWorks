using System.Collections.Generic;

namespace Fdw.Web.Calculations.Clients.Models;

/// <summary>
/// Request to execute a windowed calculation.
/// </summary>
public sealed class WindowedCalculationRequestPayload
{
    /// <summary>Gets or sets the name of the calculation entity to execute.</summary>
    public string CalculationName { get; set; } = string.Empty;

    /// <summary>Gets or sets the target field the window function operates on.</summary>
    public string TargetField { get; set; } = string.Empty;

    /// <summary>Gets or sets the window function to apply (e.g. "ROW_NUMBER", "SUM", "RANK").</summary>
    public string WindowFunction { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the output field that receives the result.</summary>
    public string OutputFieldName { get; set; } = string.Empty;

    /// <summary>Gets or sets the fields used to partition rows for the window.</summary>
    public IReadOnlyList<string> PartitionByFields { get; set; } = [];

    /// <summary>Gets or sets the fields and directions used to order rows within each partition.</summary>
    public IReadOnlyList<WindowedOrderFieldPayload> OrderByFields { get; set; } = [];
}
