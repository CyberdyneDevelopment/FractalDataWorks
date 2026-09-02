using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>Looks after the project's data. Can edit and review, but not transfer ownership.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(UniverseMemberRoles), "Steward")]
public sealed class StewardUniverseMemberRoleOption : UniverseMemberRoleBase
{
    /// <summary>Initializes a new instance of the <see cref="StewardUniverseMemberRoleOption"/> class.</summary>
    public StewardUniverseMemberRoleOption() : base("Steward")
    {
    }
}
