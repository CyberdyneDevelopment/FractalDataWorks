using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Reads the project. Cannot change it.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceMemberRoles), "Consumer")]
public sealed class ConsumerWorkspaceMemberRoleOption : WorkspaceMemberRoleBase
{
    /// <summary>Initializes a new instance of the <see cref="ConsumerWorkspaceMemberRoleOption"/> class.</summary>
    public ConsumerWorkspaceMemberRoleOption() : base("Consumer", mayView: true, mayRead: true, mayWrite: false, mayManageMembers: false, rank: 20)
    {
    }
}
