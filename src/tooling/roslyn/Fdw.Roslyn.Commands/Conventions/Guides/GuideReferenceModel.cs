using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fdw.Roslyn.Commands.Conventions.Guides;

/// <summary>
/// A require or forbid rule over a project's references.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed class GuideReferenceModel
{
    /// <summary>Gets the <c>assembly</c> value.</summary>
    [JsonPropertyName("assembly")]
    public required string Assembly { get; init; }

    /// <summary>Gets the <c>projects</c> value.</summary>
    [JsonPropertyName("projects")]
    public required string Projects { get; init; }

    /// <summary>Gets the <c>except</c> value.</summary>
    [JsonPropertyName("except")]
    public string? Except { get; init; }

    /// <summary>Gets the <c>why</c> value.</summary>
    [JsonPropertyName("why")]
    public required string Why { get; init; }

    /// <summary>Gets the <c>cite</c> value.</summary>
    [JsonPropertyName("cite")]
    public required string Cite { get; init; }
}
