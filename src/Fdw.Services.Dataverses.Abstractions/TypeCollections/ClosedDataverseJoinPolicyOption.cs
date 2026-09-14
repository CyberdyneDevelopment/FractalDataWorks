using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Requests are not accepted. Membership is by invitation only.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseJoinPolicies), "Closed")]
public sealed class ClosedDataverseJoinPolicyOption : DataverseJoinPolicyBase
{
    /// <summary>Initializes a new instance of the <see cref="ClosedDataverseJoinPolicyOption"/> class.</summary>
    public ClosedDataverseJoinPolicyOption() : base("Closed", acceptsRequests: false, autoApproves: false)
    {
    }
}
