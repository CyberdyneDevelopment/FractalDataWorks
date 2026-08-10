namespace Fdw.Services.Quality.Endpoints.Promotion;

/// <summary>
/// DTO representing the result of promoting a single item.
/// </summary>
public class PromotionItemResultDto
{
    /// <summary>Gets or sets the item type.</summary>
    public string ItemType { get; set; } = string.Empty;

    /// <summary>Gets or sets the item name.</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>Gets or sets whether promotion succeeded for this item.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets the error message if the item failed to promote.</summary>
    public string? ErrorMessage { get; set; }
}
