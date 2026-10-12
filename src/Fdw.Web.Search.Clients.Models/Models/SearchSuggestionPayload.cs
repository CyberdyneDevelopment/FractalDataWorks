namespace Fdw.Web.Search.Clients.Models;

/// <summary>
/// Represents a search suggestion for autocomplete functionality.
/// </summary>
public sealed class SearchSuggestionPayload
{
    /// <summary>
    /// Gets or sets the suggestion text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity type of the suggestion.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL to navigate to the suggested entity.
    /// </summary>
    public string Url { get; set; } = string.Empty;
}
