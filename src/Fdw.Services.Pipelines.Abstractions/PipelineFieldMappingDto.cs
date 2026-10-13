using System;
using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

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

    /// <summary>Gets or sets the target data type.</summary>
    public string? TargetType { get; set; }


    /// <summary>Gets or sets whether the mapping is enabled.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets whether the destination field is required.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Gets or sets the optional default value applied when the source field is absent.</summary>
    public string? DefaultValue { get; set; }

    /// <summary>Gets or sets the id of the mapping.</summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the chain of named Transformations the mapping's value goes through, in order (the wire member
    /// <c>transformations</c>). Each step states its <c>transformationId</c>, the <c>transformation</c> name (null when the caller cannot read that Transformation) and its <c>ordinal</c>; null when the mapping states none.
    /// </summary>
    public IList<PipelineFieldTransformationReference>? Transformations { get; set; }
}
