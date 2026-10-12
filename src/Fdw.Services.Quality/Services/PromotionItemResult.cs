namespace Fdw.Services.Quality.Services;

/// <summary>
/// Result for an individual promoted item.
/// </summary>
public sealed record PromotionItemResult(
    string ItemType,
    string ItemName,
    bool Success,
    string? ErrorMessage);