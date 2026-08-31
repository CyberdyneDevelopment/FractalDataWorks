using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>Requests are not accepted. Membership is by invitation only.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(UniverseJoinPolicies), "Closed")]
public sealed class ClosedUniverseJoinPolicyOption : UniverseJoinPolicyBase
{
    /// <summary>Initializes a new instance of the <see cref="ClosedUniverseJoinPolicyOption"/> class.</summary>
    public ClosedUniverseJoinPolicyOption() : base("Closed")
    {
    }
}
