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


    /// <summary>Gets or sets whether the mapping is enabled.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets whether the destination field is required.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Gets or sets the optional default value applied when the source field is absent.</summary>
    public string? DefaultValue { get; set; }
}
