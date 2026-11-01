using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fdw.Roslyn.Commands.Conventions.Guides;

/// <summary>
/// The reference rules of a guide.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed class GuideReferencesModel
{
    /// <summary>Gets the <c>require</c> value.</summary>
    [JsonPropertyName("require")]
    public required List<GuideReferenceModel> Require { get; init; }

    /// <summary>Gets the <c>forbid</c> value.</summary>
    [JsonPropertyName("forbid")]
    public required List<GuideReferenceModel> Forbid { get; init; }
}
