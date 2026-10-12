namespace Fdw.Operations.Endpoints;

/// <summary>
/// Request to retrieve the transitive lineage graph for a specific entity.
/// </summary>
public class LineageGraphRequest
{
    /// <summary>Gets or sets the type of entity (e.g., DataSet, Pipeline, Connection).</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the entity to retrieve lineage for.</summary>
    public string EntityName { get; set; } = string.Empty;
}
