using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Reviewed and granted — membership followed.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceMembershipRequestStatuses), "Approved")]
public sealed class ApprovedWorkspaceMembershipRequestStatusOption : WorkspaceMembershipRequestStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="ApprovedWorkspaceMembershipRequestStatusOption"/> class.</summary>
    public ApprovedWorkspaceMembershipRequestStatusOption() : base("Approved")
    {
    }
}
