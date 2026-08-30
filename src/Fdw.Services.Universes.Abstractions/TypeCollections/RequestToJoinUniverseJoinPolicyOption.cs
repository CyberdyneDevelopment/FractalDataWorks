using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>Anyone who can see it may ask; an owner or steward reviews.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(UniverseJoinPolicies), "RequestToJoin")]
public sealed class RequestToJoinUniverseJoinPolicyOption : UniverseJoinPolicyBase
{
    /// <summary>Initializes a new instance of the <see cref="RequestToJoinUniverseJoinPolicyOption"/> class.</summary>
    public RequestToJoinUniverseJoinPolicyOption() : base("RequestToJoin")
    {
    }
}
