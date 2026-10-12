using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A workspace was being created but the caller creating it could not be resolved to a user id.
/// </summary>
/// <remarks>
/// The create endpoint sits behind the <c>workspaces:write</c> policy, so an unauthenticated caller
/// cannot reach it. Arriving there with no authentication context, or with a UserId that is not a
/// Guid, is therefore an internal inconsistency rather than a rejected request — hence 500 rather
/// than a 401 or a validation failure.
///
/// Why this refuses instead of storing Guid.Empty: OwnerUserId is NOT NULL, so a zero Guid stores
/// cleanly and reads back as an owner that cannot be resolved. Ownership is also what
/// workspaces:write is to be scoped by, so a workspace with no real owner is one nobody can edit
/// once that scoping lands — including whoever created it.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "WorkspaceOwnerUnresolved", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspaceOwnerUnresolvedCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceOwnerUnresolvedCode"/> class.</summary>
    public WorkspaceOwnerUnresolvedCode()
        // 90001, not a second 90000: WorkspacesResultCodes declares ById unique, so two codes
        // sharing a number make the whole collection throw on first lookup -- which took every
        // workspace write down, not just this code's own path. Ids increment within their
        // category band (see HttpResultCodes 71000-71005); the band carries the meaning and the
        // sequence keeps them distinct.
        : base(90001, "WorkspaceOwnerUnresolved",
            ResultSeverities.ByName("Error"),
            "Workspace '{name}' cannot be created: the calling user could not be resolved ({reason})",
            isRetryable: false)
    {
    }
}
