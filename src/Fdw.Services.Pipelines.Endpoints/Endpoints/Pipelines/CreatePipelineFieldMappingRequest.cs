using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Request body describing a single field mapping within a Map transform on a create-pipeline request.
/// Maps onto <c>PipelineTransformFieldMappingConfiguration</c> (pipe.PipelineTransformFieldMapping).
/// </summary>
/// <remarks>
/// Why: implements <see cref="IFieldMapping"/> so it can flow straight into
/// <c>MapTransformType.MapSpecToConfiguration</c> (via <see cref="Fdw.Services.Etl.Abstractions.ITransformOperationSpec.FieldMappings"/>)
/// without a separate app-owned adapter type (FDW-556).
/// </remarks>
public class CreatePipelineFieldMappingRequest : IFieldMapping
{
    /// <summary>
    /// Gets or sets the mapping name (display/identity within the transform).
    /// </summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source field name read from the input record.
    /// </summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string SourceField { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the destination field name written to the output record.
    /// </summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string DestinationField { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional target data type to coerce the value to (e.g. "long", "datetime").
    /// </summary>
    public string? TargetType { get; set; }

    /// <summary>
    /// Gets or sets the optional named transform expression applied before TargetType coercion
    /// (e.g. "FromUnixMilliseconds"). Resolved against the TransformationTypes collection at runtime.
    /// </summary>
    public string? TransformExpression { get; set; }

    /// <inheritdoc/>
    /// <remarks>
    /// Why this projects <see cref="TransformExpression"/>: the create request carries one transform
    /// name and no parameter values, so a one-step chain is what it actually describes. It reports no
    /// chain when no name was supplied rather than inventing one.
    /// </remarks>
    public IReadOnlyList<IFieldMappingTransform> Transforms =>
        string.IsNullOrEmpty(TransformExpression)
            ? []
            : [new PipelineFieldMappingTransform(TransformExpression)];

    /// <summary>
    /// Gets or sets whether the source field is required (a missing required field reports an error).
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets the default value applied when the source field is null or missing.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets whether this mapping is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}
