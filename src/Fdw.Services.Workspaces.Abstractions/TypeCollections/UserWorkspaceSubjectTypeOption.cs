using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>An individual person holds the membership.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceSubjectTypes), "User")]
public sealed class UserWorkspaceSubjectTypeOption : WorkspaceSubjectTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="UserWorkspaceSubjectTypeOption"/> class.</summary>
    public UserWorkspaceSubjectTypeOption() : base("User")
    {
    }

    /// <inheritdoc />
    public override bool Holds(
        System.Guid subjectId,
        System.Guid callerUserId,
        System.Collections.Generic.IReadOnlyCollection<System.Guid> callerRoleIds)
        => subjectId == callerUserId;
}
