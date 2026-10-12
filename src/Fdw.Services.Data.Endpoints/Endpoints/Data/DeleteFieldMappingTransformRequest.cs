using System;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Detaches a transformation from a field mapping. The transformation itself is untouched.</summary>
public class DeleteFieldMappingTransformRequest
{
    /// <summary>Gets or sets the field mapping.</summary>
    public Guid FieldMappingId { get; set; }

    /// <summary>Gets or sets the name of the transformation to detach.</summary>
    public string TransformationName { get; set; } = string.Empty;
}
