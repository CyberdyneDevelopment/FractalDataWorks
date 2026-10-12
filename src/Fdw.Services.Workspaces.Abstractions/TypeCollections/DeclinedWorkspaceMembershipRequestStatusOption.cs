using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Reviewed and refused.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceMembershipRequestStatuses), "Declined")]
public sealed class DeclinedWorkspaceMembershipRequestStatusOption : WorkspaceMembershipRequestStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="DeclinedWorkspaceMembershipRequestStatusOption"/> class.</summary>
    public DeclinedWorkspaceMembershipRequestStatusOption() : base("Declined")
    {
    }
}
