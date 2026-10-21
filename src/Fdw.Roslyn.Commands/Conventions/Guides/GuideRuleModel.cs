using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fdw.Roslyn.Commands.Conventions.Guides;

/// <summary>
/// A must or must-not statement with its citation.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed class GuideRuleModel
{
    /// <summary>Gets the <c>head</c> value.</summary>
    [JsonPropertyName("head")]
    public required string Head { get; init; }

    /// <summary>Gets the <c>body</c> value.</summary>
    [JsonPropertyName("body")]
    public required string Body { get; init; }

    /// <summary>Gets the <c>cite</c> value.</summary>
    [JsonPropertyName("cite")]
    public required string Cite { get; init; }

    /// <summary>Gets the <c>analyzer</c> value.</summary>
    [JsonPropertyName("analyzer")]
    public string? Analyzer { get; init; }
}
