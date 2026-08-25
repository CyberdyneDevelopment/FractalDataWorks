using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Request to create or update a field mapping transform step.
/// </summary>
public sealed class SaveFieldMappingTransformRequest
{
    /// <summary>Gets or sets the parent field mapping identifier.</summary>
    public Guid FieldMappingId { get; set; }

    /// <summary>Gets or sets the transform type name from TransformationTypes.</summary>
    public string TransformType { get; set; } = string.Empty;

    /// <summary>Gets or sets the execution order within the transform chain.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the parameters for this transform step.</summary>
    public IList<SaveTransformParameterRequest> Parameters { get; set; } = [];
}
