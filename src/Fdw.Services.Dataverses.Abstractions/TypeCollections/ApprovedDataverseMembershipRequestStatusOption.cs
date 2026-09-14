using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Reviewed and granted — membership followed.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseMembershipRequestStatuses), "Approved")]
public sealed class ApprovedDataverseMembershipRequestStatusOption : DataverseMembershipRequestStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="ApprovedDataverseMembershipRequestStatusOption"/> class.</summary>
    public ApprovedDataverseMembershipRequestStatusOption() : base("Approved")
    {
    }
}
