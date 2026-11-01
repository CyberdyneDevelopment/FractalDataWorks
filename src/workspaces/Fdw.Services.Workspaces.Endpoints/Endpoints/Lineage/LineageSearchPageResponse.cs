using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A page of lineage search hits in the paged envelope the list endpoints use: <c>GET /lineage/search?paged=true</c>.</summary>
/// <remarks>The members are written in the order declared. <see cref="Error"/> is last: a search that fails answers with the problem and no page, so it is always null here, kept for the shape every paged list shares.</remarks>
public sealed class LineageSearchPageResponse
{
    /// <summary>Gets or sets how many matches precede this page.</summary>
    [JsonPropertyOrder(0)]
    public int Skip { get; set; }

    /// <summary>Gets or sets how many matches the page holds at most: the count that was used, capped to the maximum.</summary>
    [JsonPropertyOrder(1)]
    public int Take { get; set; }

    /// <summary>Gets or sets whether matches follow this page.</summary>
    [JsonPropertyOrder(2)]
    public bool HasMore { get; set; }

    /// <summary>Gets or sets the matches on this page, the closest names first.</summary>
    [JsonPropertyOrder(3)]
    public List<LineageSearchHitDto> Items { get; set; } = [];

    /// <summary>Gets or sets why the page could not be answered; always nothing on a page that was sent.</summary>
    [JsonPropertyOrder(4)]
    public string? Error { get; set; }
}
