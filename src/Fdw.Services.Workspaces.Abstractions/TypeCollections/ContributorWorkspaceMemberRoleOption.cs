using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Adds and edits the project's contents.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceMemberRoles), "Contributor")]
public sealed class ContributorWorkspaceMemberRoleOption : WorkspaceMemberRoleBase
{
    /// <summary>Initializes a new instance of the <see cref="ContributorWorkspaceMemberRoleOption"/> class.</summary>
    public ContributorWorkspaceMemberRoleOption() : base("Contributor", mayView: true, mayRead: true, mayWrite: true, mayManageMembers: false, rank: 30)
    {
    }
}
