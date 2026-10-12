using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Finished. Retained for reference, and what the workspace owns goes with it.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceStatuses), "Archived")]
public sealed class ArchivedWorkspaceStatusOption : WorkspaceStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="ArchivedWorkspaceStatusOption"/> class.</summary>
    public ArchivedWorkspaceStatusOption() : base("Archived")
    {
    }
}
