using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Reorders the transformations attached to a field mapping.</summary>
public class ReorderFieldMappingTransformsRequest
{
    /// <summary>Gets or sets the field mapping.</summary>
    public Guid FieldMappingId { get; set; }

    /// <summary>
    /// Gets or sets every attached transformation's name, in the new order. Each attached one must
    /// appear exactly once.
    /// </summary>
    public IList<string> TransformationNames { get; set; } = [];
}
