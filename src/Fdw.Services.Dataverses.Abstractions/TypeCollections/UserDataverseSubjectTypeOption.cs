using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>An individual person holds the membership.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseSubjectTypes), "User")]
public sealed class UserDataverseSubjectTypeOption : DataverseSubjectTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="UserDataverseSubjectTypeOption"/> class.</summary>
    public UserDataverseSubjectTypeOption() : base("User")
    {
    }

    /// <inheritdoc />
    public override bool Holds(
        System.Guid subjectId,
        System.Guid callerUserId,
        System.Collections.Generic.IReadOnlyCollection<System.Guid> callerRoleIds)
        => subjectId == callerUserId;
}
