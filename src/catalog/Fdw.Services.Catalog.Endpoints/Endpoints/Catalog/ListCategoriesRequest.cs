using System;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Request containing optional filter criteria for listing categories.</summary>
public class ListCategoriesRequest
{
    /// <summary>Gets or sets the optional entity type to filter categories by.</summary>
    public string? EntityType { get; set; }
}
