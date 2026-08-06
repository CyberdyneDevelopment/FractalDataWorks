using System;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Request DTO for deleting a field mapping transform by identifier.
/// </summary>
public class DeleteFieldMappingTransformRequest
{
    /// <summary>Gets or sets the parent field mapping identifier.</summary>
    public Guid FieldMappingId { get; set; }

    /// <summary>Gets or sets the transform identifier to delete.</summary>
    public Guid TransformId { get; set; }
}
