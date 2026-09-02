using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>
/// A role holds the membership, so everyone in the role is a member.
/// </summary>
/// <remarks>
/// Stored as the role rather than expanded into one row per current member, so someone joining the
/// role afterwards gets access without anyone re-granting it — and someone leaving loses it.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(UniverseSubjectTypes), "Role")]
public sealed class RoleUniverseSubjectTypeOption : UniverseSubjectTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="RoleUniverseSubjectTypeOption"/> class.</summary>
    public RoleUniverseSubjectTypeOption() : base("Role")
    {
    }
}
