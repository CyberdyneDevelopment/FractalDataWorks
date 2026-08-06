namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Request DTO for an aggregate measure definition within a data set composition.
/// </summary>
public class CreateDataSetAggregateRequest
{
    /// <summary>Gets or sets the name of the output column produced by this aggregate.</summary>
    public string AggregateColumnName { get; set; } = string.Empty;

    /// <summary>Gets or sets the comma-delimited list of field names to group by.</summary>
    public string GroupByFieldNames { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the aggregate function to apply (resolved via <c>AggregationFunctions</c>).</summary>
    public string AggregateFunctionName { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the input field fed into the aggregate function.</summary>
    public string InputFieldName { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional human-facing label for this aggregate column.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets an optional description of the business meaning of this aggregate.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the display/execution order of this aggregate definition.</summary>
    public int Ordinal { get; set; }
}
