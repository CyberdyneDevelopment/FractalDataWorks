namespace Fdw.Operations.Endpoints;

/// <summary>
/// Request to retrieve field-level lineage for a specific field within an entity.
/// </summary>
public class LineageFieldRequest
{
    /// <summary>Gets or sets the type of entity (e.g., DataSet, Pipeline).</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the entity containing the field.</summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the field to retrieve lineage for.</summary>
    public string FieldName { get; set; } = string.Empty;
}
