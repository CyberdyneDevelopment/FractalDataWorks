using System;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// DTO representing an aggregate measure definition composed on a data set.
/// </summary>
public class DataSetAggregateDto
{
    /// <summary>Gets or sets the aggregate definition identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of the output column produced by this aggregate.</summary>
    public string AggregateColumnName { get; set; } = string.Empty;

    /// <summary>Gets or sets the comma-delimited list of field names to group by.</summary>
    public string GroupByFieldNames { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the aggregate function applied (a resolved <c>AggregationFunctions</c> member).</summary>
    public string AggregateFunctionName { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the input field fed into the aggregate function.</summary>
    public string InputFieldName { get; set; } = string.Empty;

    /// <summary>Gets or sets the human-facing label for this aggregate column.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the description of the business meaning of this aggregate.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the display/execution order of this aggregate definition.</summary>
    public int Ordinal { get; set; }
}
