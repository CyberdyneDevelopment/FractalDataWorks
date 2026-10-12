using System;

namespace Fdw.Services.Quality.Endpoints.Promotion;

/// <summary>
/// Represents a single item to include in a new promotion request.
/// </summary>
public class CreatePromotionItemRequest
{
    /// <summary>Gets or sets the item type (e.g., "Pipeline", "Connection", "Schedule").</summary>
    public string ItemType { get; set; } = string.Empty;

    /// <summary>Gets or sets the item name.</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>Gets or sets the item's logical identifier.</summary>
    public Guid? ItemId { get; set; }
}
