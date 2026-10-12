using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>In use.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceStatuses), "Active")]
public sealed class ActiveWorkspaceStatusOption : WorkspaceStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="ActiveWorkspaceStatusOption"/> class.</summary>
    public ActiveWorkspaceStatusOption() : base("Active")
    {
    }
}
