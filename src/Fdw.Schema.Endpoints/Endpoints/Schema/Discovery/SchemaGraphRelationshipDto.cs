namespace Fdw.Schema.Endpoints.Discovery;

/// <summary>
/// A relationship between schema graph entities.
/// </summary>
public class SchemaGraphRelationshipDto
{
    /// <summary>Gets or sets the source entity full name.</summary>
    public string SourceEntity { get; set; } = string.Empty;

    /// <summary>Gets or sets the target entity full name.</summary>
    public string TargetEntity { get; set; } = string.Empty;

    /// <summary>Gets or sets the source column name.</summary>
    public string SourceColumn { get; set; } = string.Empty;

    /// <summary>Gets or sets the target column name.</summary>
    public string TargetColumn { get; set; } = string.Empty;
}
