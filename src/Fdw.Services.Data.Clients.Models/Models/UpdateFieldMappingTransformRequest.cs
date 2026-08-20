using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>Changes a transform already in a field mapping's chain.</summary>
/// <remarks>
/// Why this exists beside <see cref="SaveFieldMappingTransformRequest"/>: that one names the mapping
/// a transform belongs to but never the transform itself, so the save path could only ever insert.
/// Changing a transform meant deleting it and adding it back, which loses its position in the chain
/// unless the caller reorders afterwards.
/// </remarks>
public sealed class UpdateFieldMappingTransformRequest
{
    /// <summary>Gets or sets the field mapping whose chain the transform belongs to.</summary>
    public Guid FieldMappingId { get; set; }

    /// <summary>Gets or sets the transform being changed.</summary>
    public Guid TransformId { get; set; }

    /// <summary>Gets or sets the transform type to apply, e.g. Trim or ToUpper.</summary>
    public string TransformType { get; set; } = string.Empty;

    /// <summary>Gets or sets the transform's position in the chain.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the parameters the transform is applied with.</summary>
    public IList<SaveTransformParameterRequest> Parameters { get; set; } = [];
}
