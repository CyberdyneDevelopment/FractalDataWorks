namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// Read-only surface for a single aggregation within an Aggregate transform request.
/// </summary>
public interface IAggregationSpec
{
    /// <summary>Gets the source field to aggregate.</summary>
    string SourceField { get; }

    /// <summary>Gets the aggregate function name (resolved against <c>AggregateFunctions</c>).</summary>
    string Function { get; }

    /// <summary>Gets the output field name.</summary>
    string OutputField { get; }
}
