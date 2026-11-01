using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A resource was attached to a workspace with a resourceType that no package registered as a resource kind (FDW-1053).
/// </summary>
/// <remarks>
/// Caller-input validation — HTTP 400. The kinds are declared by the packages that own the resources, so the list is what this host actually has, and the refusal names it. Not the same as <c>WorkspaceResourceKindNotAttachable</c>: there the kind is registered.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "WorkspaceResourceKindUnknown", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspaceResourceKindUnknownCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceResourceKindUnknownCode"/> class.</summary>
    public WorkspaceResourceKindUnknownCode()
        : base(20006, "WorkspaceResourceKindUnknown",
            ResultSeverities.ByName("Error"),
            "Workspace '{name}' rejected: resourceType '{value}' is not a resource kind this host knows; the kinds are {kinds}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
