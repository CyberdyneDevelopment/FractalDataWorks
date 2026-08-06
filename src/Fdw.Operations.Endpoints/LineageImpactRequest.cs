namespace Fdw.Operations.Endpoints;

/// <summary>
/// Request to retrieve the lineage impact analysis for a named entity.
/// </summary>
public class LineageImpactRequest
{
    /// <summary>Gets or sets the name of the entity to analyze impact for.</summary>
    public string EntityName { get; set; } = string.Empty;
}
