namespace Fdw.Data.DataSets;

/// <summary>
/// One aggregate measure within an <see cref="AggregateConfiguration"/>.
/// </summary>
public sealed class AggregateMeasure
{
    /// <summary>Gets or sets the name of the source field fed into the aggregate function.</summary>
    /// <remarks>E.g., "Amount" for SUM, "Id" for COUNT.</remarks>
    public string SourceField { get; set; } = string.Empty;

    /// <summary>Gets or sets the aggregate function to apply.</summary>
    /// <value>One of: "Sum", "Avg", "Count", "Min", "Max".</value>
    public string AggregateFunction { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the output column produced by this measure.</summary>
    /// <remarks>E.g., "TotalSales", "TransactionCount".</remarks>
    public string OutputName { get; set; } = string.Empty;
}
