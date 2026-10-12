using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Being set up; not yet in use.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceStatuses), "Draft")]
public sealed class DraftWorkspaceStatusOption : WorkspaceStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="DraftWorkspaceStatusOption"/> class.</summary>
    public DraftWorkspaceStatusOption() : base("Draft")
    {
    }
}
