namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Response DTO for a single field mapping within a <see cref="PipelineTransformDto"/> (Map transform),
/// read from the composed aggregate's typed <c>FieldMappings</c> cascade children.
/// </summary>
public class PipelineFieldMappingDto
{
    /// <summary>Gets or sets the field mapping name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the source field name.</summary>
    public string SourceField { get; set; } = string.Empty;

    /// <summary>Gets or sets the destination field name.</summary>
    public string DestinationField { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional transform expression.</summary>
    public string? TransformExpression { get; set; }

    /// <summary>Gets or sets the target data type.</summary>
    public string? TargetType { get; set; }

    // Why: these three are persisted on the mapping (IFieldMapping declares them) but were omitted from this
    // response, making GET lossy: a mapping saved with IsEnabled=false came back with the field ABSENT, the
    // client materialised its `= true` initializer, and re-saving silently re-ENABLED it. Same class of data
    // loss for IsRequired and DefaultValue. The response must carry everything the request can set.

    /// <summary>Gets or sets whether the mapping is enabled.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets whether the destination field is required.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Gets or sets the optional default value applied when the source field is absent.</summary>
    public string? DefaultValue { get; set; }
}
