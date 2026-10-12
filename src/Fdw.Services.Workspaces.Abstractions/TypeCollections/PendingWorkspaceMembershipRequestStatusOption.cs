using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Awaiting review.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceMembershipRequestStatuses), "Pending")]
public sealed class PendingWorkspaceMembershipRequestStatusOption : WorkspaceMembershipRequestStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="PendingWorkspaceMembershipRequestStatusOption"/> class.</summary>
    public PendingWorkspaceMembershipRequestStatusOption() : base("Pending")
    {
    }
}
