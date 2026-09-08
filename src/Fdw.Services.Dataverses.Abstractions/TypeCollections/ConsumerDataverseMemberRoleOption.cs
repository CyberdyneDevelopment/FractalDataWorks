using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Reads the project. Cannot change it.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseMemberRoles), "Consumer")]
public sealed class ConsumerDataverseMemberRoleOption : DataverseMemberRoleBase
{
    /// <summary>Initializes a new instance of the <see cref="ConsumerDataverseMemberRoleOption"/> class.</summary>
    public ConsumerDataverseMemberRoleOption() : base("Consumer", mayWrite: false)
    {
    }
}
