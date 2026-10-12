using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A workspace's AutoApproveRole was missing, not a member role, or not allowed with its join policy.
/// </summary>
/// <remarks>
/// Caller-input validation, HTTP 400. An AutoApprove workspace states the one role it grants to whoever
/// asks (FDW-1070); there is no default. The message is shown to the caller.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "WorkspaceAutoApproveRoleInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspaceAutoApproveRoleInvalidCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceAutoApproveRoleInvalidCode"/> class.</summary>
    public WorkspaceAutoApproveRoleInvalidCode()
        : base(20010, "WorkspaceAutoApproveRoleInvalid",
            ResultSeverities.ByName("Error"),
            "Workspace '{name}' rejected: autoApproveRole '{value}' {reason}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what was refused and why: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
