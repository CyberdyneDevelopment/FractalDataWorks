using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Anyone who can see it becomes a member on asking, with no review.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseJoinPolicies), "AutoApprove")]
public sealed class AutoApproveDataverseJoinPolicyOption : DataverseJoinPolicyBase
{
    /// <summary>Initializes a new instance of the <see cref="AutoApproveDataverseJoinPolicyOption"/> class.</summary>
    public AutoApproveDataverseJoinPolicyOption() : base("AutoApprove", acceptsRequests: true, autoApproves: true)
    {
    }
}
