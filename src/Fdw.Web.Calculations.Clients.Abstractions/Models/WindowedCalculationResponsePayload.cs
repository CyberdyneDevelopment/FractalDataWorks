namespace Fdw.Web.Calculations.Clients.Models;

/// <summary>
/// Response from a windowed calculation execution.
/// </summary>
public sealed class WindowedCalculationResponsePayload
{
    /// <summary>Gets or sets the name of the calculation that was executed.</summary>
    public string CalculationName { get; set; } = string.Empty;

    /// <summary>Gets or sets the window function that was applied.</summary>
    public string WindowFunction { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the result field.</summary>
    public string ResultField { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of partitions processed.</summary>
    public int PartitionCount { get; set; }

    /// <summary>Gets or sets the total number of rows processed.</summary>
    public int RowCount { get; set; }

    /// <summary>Gets or sets the JSON-serialized result rows.</summary>
    public string ResultJson { get; set; } = string.Empty;

    /// <summary>Gets or sets the execution duration in milliseconds.</summary>
    public long DurationMs { get; set; }
}
