using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Looks after the project's data. Can edit and review, but not transfer ownership.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseMemberRoles), "Steward")]
public sealed class StewardDataverseMemberRoleOption : DataverseMemberRoleBase
{
    /// <summary>Initializes a new instance of the <see cref="StewardDataverseMemberRoleOption"/> class.</summary>
    public StewardDataverseMemberRoleOption() : base("Steward", mayWrite: true)
    {
    }
}
