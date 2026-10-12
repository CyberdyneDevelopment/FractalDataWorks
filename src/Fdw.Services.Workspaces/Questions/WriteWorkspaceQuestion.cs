using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces;

/// <summary>Whether the caller can change the workspace.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceQuestions), "Write")]
public sealed class WriteWorkspaceQuestion : WorkspaceQuestionBase
{
    /// <summary>Initializes a new instance of the <see cref="WriteWorkspaceQuestion"/> class.</summary>
    public WriteWorkspaceQuestion()
        : base("Write", "the caller neither owns it, holds a membership that may change it, nor holds workspaces:write")
    {
    }

    /// <inheritdoc />
    public override bool Grants(WorkspaceStanding held)
        => held.IsOwner || held.Holds("workspaces:write") || held.Roles.Any(role => role.MayWrite);

    /// <inheritdoc />
    /// <remarks>404 when the caller may not view it; 403 once they may and only the change is refused.</remarks>
    public override string RefusalCode(bool views) => views ? "WorkspaceWriteNotPermitted" : "WorkspaceNotVisible";
}
