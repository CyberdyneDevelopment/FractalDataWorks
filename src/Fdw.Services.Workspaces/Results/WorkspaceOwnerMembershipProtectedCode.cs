using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// The owner's own membership of a workspace cannot be removed or demoted.
/// </summary>
/// <remarks>
/// Caller-input validation, HTTP 400. The workspace's owner (<c>OwnerUserId</c>) is always an owner; a member row
/// that names the owner at a lower role, or removes it, would show a workspace whose listed members
/// contradict who owns it (FDW-1071). The message is shown to the caller.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "WorkspaceOwnerMembershipProtected", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspaceOwnerMembershipProtectedCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceOwnerMembershipProtectedCode"/> class.</summary>
    public WorkspaceOwnerMembershipProtectedCode()
        : base(20009, "WorkspaceOwnerMembershipProtected",
            ResultSeverities.ByName("Error"),
            "Workspace '{name}' refused: the owner's membership cannot be {action}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what was refused and why: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
