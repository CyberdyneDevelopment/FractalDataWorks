using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Reviewed and refused.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseMembershipRequestStatuses), "Declined")]
public sealed class DeclinedDataverseMembershipRequestStatusOption : DataverseMembershipRequestStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="DeclinedDataverseMembershipRequestStatusOption"/> class.</summary>
    public DeclinedDataverseMembershipRequestStatusOption() : base("Declined")
    {
    }
}
