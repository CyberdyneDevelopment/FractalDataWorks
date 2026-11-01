using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Search.Results;

/// <summary>
/// A single divergent member discovered during family-drift analysis.
/// </summary>
public sealed class FamilyDriftMember
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FamilyDriftMember"/> class.
    /// </summary>
    public FamilyDriftMember(
        string memberName,
        string signature,
        string bucket,
        IReadOnlyList<string> presentIn,
        IReadOnlyList<string> missingFrom)
    {
        MemberName = memberName;
        Signature = signature;
        Bucket = bucket;
        PresentIn = presentIn;
        MissingFrom = missingFrom;
    }

    /// <summary>Gets the member name (without parameters).</summary>
    public string MemberName { get; }

    /// <summary>Gets the full member signature.</summary>
    public string Signature { get; }

    /// <summary>
    /// Gets the drift bucket: Hoist (N-of-N — promote to base) /
    /// MostHave ((N-1)-of-N — add to outlier or remove from siblings) /
    /// Bloat (1-of-N — likely remove) / Mixed (intermediate split).
    /// </summary>
    public string Bucket { get; }

    /// <summary>Gets the implementations that contain this member.</summary>
    public IReadOnlyList<string> PresentIn { get; }

    /// <summary>Gets the implementations that do NOT contain this member.</summary>
    public IReadOnlyList<string> MissingFrom { get; }
}
