using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Lapsed unreviewed, past its ExpiresAt.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseMembershipRequestStatuses), "Expired")]
public sealed class ExpiredDataverseMembershipRequestStatusOption : DataverseMembershipRequestStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="ExpiredDataverseMembershipRequestStatusOption"/> class.</summary>
    public ExpiredDataverseMembershipRequestStatusOption() : base("Expired")
    {
    }
}
