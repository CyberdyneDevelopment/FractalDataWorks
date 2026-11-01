using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>The types of category (FDW-1465).</summary>
public sealed class CategoryTypesPayload
{
    /// <summary>Gets or sets the types, in name order.</summary>
    public IList<CategoryTypePayload> Types { get; set; } = [];
}
