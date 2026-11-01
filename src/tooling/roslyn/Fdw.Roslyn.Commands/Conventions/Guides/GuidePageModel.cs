using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fdw.Roslyn.Commands.Conventions.Guides;

/// <summary>
/// One page of the implementing document. A page with no guide object is not a guide.
/// </summary>
internal sealed class GuidePageModel
{
    /// <summary>Gets the <c>id</c> value.</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>Gets the <c>label</c> value.</summary>
    [JsonPropertyName("label")]
    public required string Label { get; init; }

    /// <summary>Gets the <c>title</c> value.</summary>
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    /// <summary>Gets the <c>guide</c> value.</summary>
    [JsonPropertyName("guide")]
    public GuideBodyModel? Guide { get; init; }
}
