using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>Reorders the transformations attached to a field mapping.</summary>
public sealed class ReorderTransformationsRequest
{
    /// <summary>
    /// Gets or sets every attached transformation's name, in the new order. Each attached one must
    /// appear exactly once.
    /// </summary>
    public IList<string> TransformationNames { get; set; } = [];
}
