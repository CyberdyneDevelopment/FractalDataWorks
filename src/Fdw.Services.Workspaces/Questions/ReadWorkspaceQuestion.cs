using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces;

/// <summary>Whether the caller can query the rows of the data sets attached to the workspace.</summary>
/// <remarks>
/// Visibility never grants rows, so a principal confined by membership to one workspace cannot read
/// another just because it is Open (FDW-876).
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceQuestions), "Read")]
public sealed class ReadWorkspaceQuestion : WorkspaceQuestionBase
{
    /// <summary>Initializes a new instance of the <see cref="ReadWorkspaceQuestion"/> class.</summary>
    public ReadWorkspaceQuestion()
        : base("Read", "the caller neither owns it, holds a membership that may read it, nor holds workspaces:query or workspaces/{id}:query")
    {
    }

    /// <inheritdoc />
    public override bool Grants(WorkspaceStanding held)
        => held.IsOwner || held.Holds("workspaces:query") || held.HoldsForThis(WorkspacePermissionNames.QueryAction) || held.Roles.Any(role => role.MayRead);

    /// <inheritdoc />
    /// <remarks>Rows keep their own code: the query route turns it into its own 404 (WorkspaceNotReadable).</remarks>
    public override string RefusalCode(bool views) => "WorkspaceReadNotPermitted";
}
