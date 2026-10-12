using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// A transformation attached to a field mapping, in chain order (FDW-846). The attachment itself is
/// only the field mapping, the transformation and the position; the rest describes the transformation.
/// </summary>
public sealed class FieldMappingTransformPayload
{
    /// <summary>Gets or sets the field mapping.</summary>
    public Guid FieldMappingId { get; set; }

    /// <summary>Gets or sets the attached transformation's name.</summary>
    public string TransformationName { get; set; } = string.Empty;

    /// <summary>Gets or sets the attached transformation's operation.</summary>
    public string TransformationType { get; set; } = string.Empty;

    /// <summary>Gets or sets the position in the chain; applied in ascending order.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets whether the attached transformation is a seeded standard one.</summary>
    public bool IsSystemProvided { get; set; }

    /// <summary>Gets or sets the attached transformation's parameter values.</summary>
    public IReadOnlyList<TransformationParameterPayload> Parameters { get; set; } = [];
}
