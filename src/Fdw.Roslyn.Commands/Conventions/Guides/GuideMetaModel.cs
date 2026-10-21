using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fdw.Roslyn.Commands.Conventions.Guides;

/// <summary>
/// The provenance stamp of the implementing document.
/// </summary>
internal sealed class GuideMetaModel
{
    /// <summary>Gets the <c>repo</c> value.</summary>
    [JsonPropertyName("repo")]
    public required string Repo { get; init; }

    /// <summary>Gets the <c>commit</c> value.</summary>
    [JsonPropertyName("commit")]
    public required string Commit { get; init; }

    /// <summary>Gets the <c>verified</c> value.</summary>
    [JsonPropertyName("verified")]
    public required string Verified { get; init; }

    /// <summary>Gets the <c>ref</c> value.</summary>
    [JsonPropertyName("ref")]
    public string? Ref { get; init; }
}
