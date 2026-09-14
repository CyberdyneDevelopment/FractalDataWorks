using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Anyone who can see it may ask; an owner or steward reviews.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseJoinPolicies), "RequestToJoin")]
public sealed class RequestToJoinDataverseJoinPolicyOption : DataverseJoinPolicyBase
{
    /// <summary>Initializes a new instance of the <see cref="RequestToJoinDataverseJoinPolicyOption"/> class.</summary>
    public RequestToJoinDataverseJoinPolicyOption() : base("RequestToJoin", acceptsRequests: true, autoApproves: false)
    {
    }
}
