using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>Adds and edits the project's contents.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(UniverseMemberRoles), "Contributor")]
public sealed class ContributorUniverseMemberRoleOption : UniverseMemberRoleBase
{
    /// <summary>Initializes a new instance of the <see cref="ContributorUniverseMemberRoleOption"/> class.</summary>
    public ContributorUniverseMemberRoleOption() : base("Contributor")
    {
    }
}
