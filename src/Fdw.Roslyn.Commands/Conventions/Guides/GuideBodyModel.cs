using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fdw.Roslyn.Commands.Conventions.Guides;

/// <summary>
/// The typed guide object of one page of the implementing document.
/// </summary>
/// <remarks>
/// Closed: an unknown key fails the read, as tools/guides.py refuses it, so a typo cannot silently drop a
/// rule. The page and the meta stamp stay open because they also carry the renderer's own keys.
/// </remarks>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed class GuideBodyModel
{
    /// <summary>Gets the <c>implements</c> value.</summary>
    [JsonPropertyName("implements")]
    public required List<GuideTypeModel> Implements { get; init; }

    /// <summary>Gets the <c>exemplars</c> value.</summary>
    [JsonPropertyName("exemplars")]
    public required List<GuideExemplarModel> Exemplars { get; init; }

    /// <summary>Gets the <c>references</c> value.</summary>
    [JsonPropertyName("references")]
    public required GuideReferencesModel References { get; init; }

    /// <summary>Gets the <c>must</c> value.</summary>
    [JsonPropertyName("must")]
    public required List<GuideRuleModel> Must { get; init; }

    /// <summary>Gets the <c>mustNot</c> value.</summary>
    [JsonPropertyName("mustNot")]
    public required List<GuideRuleModel> MustNot { get; init; }

    /// <summary>Gets the <c>see</c> value.</summary>
    [JsonPropertyName("see")]
    public required List<GuideSeeModel> See { get; init; }
}
