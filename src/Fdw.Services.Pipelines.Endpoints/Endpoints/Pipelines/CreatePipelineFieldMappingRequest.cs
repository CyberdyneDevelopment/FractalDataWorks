using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;

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
public class CreatePipelineFieldMappingRequest : IFieldMapping, IFieldMappingChainSpec
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
    /// Gets or sets the chain of named Transformations the mapping applies to its value (the request's <c>transformations</c>).
    /// </summary>
    /// <remarks>
    /// Why each step names its Transformation by name OR by id (exactly one): the name is what a person writes and the id is
    /// what a read-back returns. The pipeline stores the id.
    /// </remarks>
    [JsonPropertyName("transformations")]
    public IList<PipelineFieldTransformationReference>? TransformationChain { get; set; }

    // Why the chain is exposed to the stage mapper through its own interface: a stage kind that reads a mapping should not
    // have to know about chains, and the mapper (which resolves names to ids) reads them here. An absent chain and an empty
    // one are the same thing.
    [JsonIgnore]
    IReadOnlyList<IFieldTransformationReferenceSpec> IFieldMappingChainSpec.TransformationReferences =>
        TransformationChain is null
            ? []
            : [.. TransformationChain.Select(reference => new FieldTransformationReferenceSpec(
                string.IsNullOrWhiteSpace(reference.Transformation) ? null : reference.Transformation,
                reference.TransformationId,
                reference.Ordinal))];

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
