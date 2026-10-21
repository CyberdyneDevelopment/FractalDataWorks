using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>The category tree: its top-level categories (each the top of one named hierarchy), each with everything under it (FDW-1465).</summary>
public sealed class CategoryTreePayload
{
    /// <summary>Gets or sets the top-level categories, in display-name order.</summary>
    public IList<CategoryNodePayload> Categories { get; set; } = [];
}
