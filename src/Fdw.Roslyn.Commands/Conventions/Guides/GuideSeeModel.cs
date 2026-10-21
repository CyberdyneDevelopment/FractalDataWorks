using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fdw.Roslyn.Commands.Conventions.Guides;

/// <summary>
/// A pointer to where the rest of a subject is documented.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed class GuideSeeModel
{
    /// <summary>Gets the <c>doc</c> value.</summary>
    [JsonPropertyName("doc")]
    public required string Doc { get; init; }

    /// <summary>Gets the <c>page</c> value.</summary>
    [JsonPropertyName("page")]
    public required string Page { get; init; }

    /// <summary>Gets the <c>why</c> value.</summary>
    [JsonPropertyName("why")]
    public required string Why { get; init; }
}
