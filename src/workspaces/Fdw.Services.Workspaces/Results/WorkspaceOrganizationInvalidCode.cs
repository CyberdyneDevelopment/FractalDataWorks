using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A workspace's organization was not an organization of the workspace's tenant, or could not be judged.
/// </summary>
/// <remarks>
/// Caller-input validation, HTTP 400. An organization that does not exist and one that belongs to another tenant
/// are answered alike, so a caller cannot find out which organizations other tenants hold (FDW-2338). The message is
/// shown to the caller.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "WorkspaceOrganizationInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspaceOrganizationInvalidCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceOrganizationInvalidCode"/> class.</summary>
    public WorkspaceOrganizationInvalidCode()
        : base(20011, "WorkspaceOrganizationInvalid",
            ResultSeverities.ByName("Error"),
            "Workspace '{name}' rejected: organizationId '{value}' {reason}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what was refused and why: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
