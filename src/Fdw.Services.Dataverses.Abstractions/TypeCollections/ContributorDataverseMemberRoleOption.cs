using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Adds and edits the project's contents.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseMemberRoles), "Contributor")]
public sealed class ContributorDataverseMemberRoleOption : DataverseMemberRoleBase
{
    /// <summary>Initializes a new instance of the <see cref="ContributorDataverseMemberRoleOption"/> class.</summary>
    public ContributorDataverseMemberRoleOption() : base("Contributor", mayWrite: true)
    {
    }
}
