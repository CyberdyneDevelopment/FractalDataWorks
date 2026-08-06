using System;

namespace Fdw.Services.Quality.Endpoints.Promotion;

/// <summary>
/// DTO representing a single item within a promotion request.
/// </summary>
public class PromotionItemDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the item type (e.g., "Pipeline", "Connection", "Schedule").</summary>
    public string ItemType { get; set; } = string.Empty;

    /// <summary>Gets or sets the item name.</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>Gets or sets the item's logical identifier.</summary>
    public Guid? ItemId { get; set; }
}
