using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Lapsed unreviewed, past its ExpiresAt.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceMembershipRequestStatuses), "Expired")]
public sealed class ExpiredWorkspaceMembershipRequestStatusOption : WorkspaceMembershipRequestStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="ExpiredWorkspaceMembershipRequestStatusOption"/> class.</summary>
    public ExpiredWorkspaceMembershipRequestStatusOption() : base("Expired")
    {
    }
}
