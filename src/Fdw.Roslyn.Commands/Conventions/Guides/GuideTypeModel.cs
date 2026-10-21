using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fdw.Roslyn.Commands.Conventions.Guides;

/// <summary>
/// A type a guide applies to, with the line that declares it.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed class GuideTypeModel
{
    /// <summary>Gets the <c>type</c> value.</summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>Gets the <c>cite</c> value.</summary>
    [JsonPropertyName("cite")]
    public required string Cite { get; init; }
}
