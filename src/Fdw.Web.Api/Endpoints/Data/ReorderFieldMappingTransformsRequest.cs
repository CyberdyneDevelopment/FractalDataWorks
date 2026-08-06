using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Request DTO for reordering transforms in a field mapping's transform chain.
/// </summary>
public class ReorderFieldMappingTransformsRequest
{
    /// <summary>Gets or sets the parent field mapping identifier.</summary>
    public Guid FieldMappingId { get; set; }

    /// <summary>Gets or sets the ordered list of transform identifiers in the desired order.</summary>
    public IList<Guid> TransformIds { get; set; } = [];
}
