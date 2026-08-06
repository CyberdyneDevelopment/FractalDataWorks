using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Request to reorder transforms in a field mapping's transform chain.
/// The TransformIds list specifies the new order.
/// </summary>
public sealed class ReorderTransformsRequest
{
    /// <summary>Gets or sets the ordered list of transform identifiers in the desired order.</summary>
    public IList<Guid> TransformIds { get; set; } = [];
}
