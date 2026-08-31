using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>Reads the project. Cannot change it.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(UniverseMemberRoles), "Consumer")]
public sealed class ConsumerUniverseMemberRoleOption : UniverseMemberRoleBase
{
    /// <summary>Initializes a new instance of the <see cref="ConsumerUniverseMemberRoleOption"/> class.</summary>
    public ConsumerUniverseMemberRoleOption() : base("Consumer")
    {
    }
}
