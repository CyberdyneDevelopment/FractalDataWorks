using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Pulled back by the person who asked, before anyone reviewed it.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseMembershipRequestStatuses), "Withdrawn")]
public sealed class WithdrawnDataverseMembershipRequestStatusOption : DataverseMembershipRequestStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="WithdrawnDataverseMembershipRequestStatusOption"/> class.</summary>
    public WithdrawnDataverseMembershipRequestStatusOption() : base("Withdrawn")
    {
    }
}
