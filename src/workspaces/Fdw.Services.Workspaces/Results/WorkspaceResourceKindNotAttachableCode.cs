using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A resource of a registered kind was refused because nothing can yet resolve its label (FDW-1053).
/// </summary>
/// <remarks>
/// Caller-input validation — HTTP 400. The attachment's name is the resource's own label and is never invented, so a kind whose label cannot be resolved is refused with this reason instead of the false claim that the kind is not registered.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "WorkspaceResourceKindNotAttachable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspaceResourceKindNotAttachableCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceResourceKindNotAttachableCode"/> class.</summary>
    public WorkspaceResourceKindNotAttachableCode()
        : base(20007, "WorkspaceResourceKindNotAttachable",
            ResultSeverities.ByName("Error"),
            "Workspace '{name}' rejected: a {kind} cannot be attached yet; its kind is registered but no label resolver is wired for it",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
