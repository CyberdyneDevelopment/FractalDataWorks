using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Represents a single transform step in a field mapping's transform chain.
/// </summary>
public sealed class FieldMappingTransformPayload
{
    /// <summary>Gets or sets the transform identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the parent field mapping identifier.</summary>
    public Guid FieldMappingId { get; set; }

    /// <summary>Gets or sets the transform type name from DataTransformerTypes.</summary>
    public string TransformType { get; set; } = string.Empty;

    /// <summary>Gets or sets the execution order within the transform chain.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the parameters for this transform step.</summary>
    public IReadOnlyList<FieldMappingTransformParameterPayload> Parameters { get; set; } = [];
}
