using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>Anyone who can see it becomes a member on asking, with no review.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(UniverseJoinPolicies), "AutoApprove")]
public sealed class AutoApproveUniverseJoinPolicyOption : UniverseJoinPolicyBase
{
    /// <summary>Initializes a new instance of the <see cref="AutoApproveUniverseJoinPolicyOption"/> class.</summary>
    public AutoApproveUniverseJoinPolicyOption() : base("AutoApprove")
    {
    }
}
