using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fdw.Roslyn.Commands.Conventions.Guides;

/// <summary>
/// A real implementation a guide names as the one to copy.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed class GuideExemplarModel
{
    /// <summary>Gets the <c>type</c> value.</summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>Gets the <c>role</c> value.</summary>
    [JsonPropertyName("role")]
    public required string Role { get; init; }

    /// <summary>Gets the <c>cite</c> value.</summary>
    [JsonPropertyName("cite")]
    public required string Cite { get; init; }
}
