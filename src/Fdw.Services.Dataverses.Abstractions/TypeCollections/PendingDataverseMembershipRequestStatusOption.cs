using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Awaiting review.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseMembershipRequestStatuses), "Pending")]
public sealed class PendingDataverseMembershipRequestStatusOption : DataverseMembershipRequestStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="PendingDataverseMembershipRequestStatusOption"/> class.</summary>
    public PendingDataverseMembershipRequestStatusOption() : base("Pending")
    {
    }
}
