namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Payload for API requests that add or remove sources, joins, calculations, and aggregations
/// during DataSet workbench composition.
/// </summary>
public sealed class DataSetCompositionOperationPayload
{
    /// <summary>Gets or sets the type of composition operation to perform.</summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>Gets or sets the source alias name. Required for source operations.</summary>
    public string? SourceName { get; set; }

    /// <summary>Gets or sets the DataStore name. Required for AddSource.</summary>
    public string? SourceDataStoreName { get; set; }

    /// <summary>Gets or sets the schema/path within the DataStore. Required for AddSource.</summary>
    public string? SourceDataPath { get; set; }

    /// <summary>Gets or sets the container (table) name within the path. Required for AddSource.</summary>
    public string? SourceContainerName { get; set; }

    /// <summary>Gets or sets the join target (right) key field name. Required for join operations.</summary>
    public string? JoinTargetField { get; set; }

    /// <summary>Gets or sets the join source (left) key field name. Required for join operations.</summary>
    public string? JoinSourceField { get; set; }

    /// <summary>Gets or sets the join type. Used for AddJoin.</summary>
    public string? JoinType { get; set; }

    /// <summary>Gets or sets the calculated field name. Required for calculation operations.</summary>
    public string? CalculationName { get; set; }

    /// <summary>Gets or sets the formula expression. Required for AddCalculation.</summary>
    public string? CalculationFormula { get; set; }

    /// <summary>Gets or sets the output data type of the calculated field. Required for AddCalculation.</summary>
    public string? CalculationDataType { get; set; }

    /// <summary>Gets or sets the aggregation output field name. Required for aggregation operations.</summary>
    public string? AggregationName { get; set; }

    /// <summary>Gets or sets the aggregation function. Required for AddAggregation.</summary>
    public string? AggregationFunction { get; set; }

    /// <summary>Gets or sets the source field that the aggregation function is applied to. Required for AddAggregation.</summary>
    public string? AggregationSourceField { get; set; }
}
