namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Statistical summary for a numeric column.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class StatSet
{
    /// <summary>Gets or sets the total number of rows.</summary>
    public long Count { get; set; }

    /// <summary>Gets or sets the sum of all values.</summary>
    public double Sum { get; set; }

    /// <summary>Gets or sets the arithmetic mean.</summary>
    public double Mean { get; set; }

    /// <summary>Gets or sets the median (50th percentile).</summary>
    public double Median { get; set; }

    /// <summary>Gets or sets the standard deviation.</summary>
    public double StdDev { get; set; }

    /// <summary>Gets or sets the minimum value.</summary>
    public double Min { get; set; }

    /// <summary>Gets or sets the maximum value.</summary>
    public double Max { get; set; }

    /// <summary>Gets or sets the 25th percentile.</summary>
    public double P25 { get; set; }

    /// <summary>Gets or sets the 75th percentile.</summary>
    public double P75 { get; set; }

    /// <summary>Gets or sets the 95th percentile.</summary>
    public double P95 { get; set; }

    /// <summary>Gets or sets the count of null values.</summary>
    public long NullCount { get; set; }

    /// <summary>Gets or sets the count of distinct values.</summary>
    public long DistinctCount { get; set; }
}
