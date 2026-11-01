namespace Fdw.Services.Quality.Services;

/// <summary>
/// A catalog search result item.
/// </summary>
public sealed record CatalogSearchResult(
    string Type,
    string Name,
    string? Description,
    double Relevance);