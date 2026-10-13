using System;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>Attaches an existing transformation to a field mapping at a position in its chain.</summary>
public sealed class SaveFieldMappingTransformationRequest
{
    /// <summary>Gets or sets the field mapping, from the route.</summary>
    public Guid FieldMappingId { get; set; }

    /// <summary>Gets or sets the name of the transformation to attach (see <c>/transformations</c>).</summary>
    public string TransformationName { get; set; } = string.Empty;

    /// <summary>Gets or sets the position in the chain; applied in ascending order.</summary>
    public int Ordinal { get; set; }
}
