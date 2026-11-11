using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>The type leaves a member of the abstraction unimplemented.</summary>
[TypeOption(typeof(AuditFindingKinds), "UnimplementedMember")]
[ExcludeFromCodeCoverage]
public sealed class UnimplementedMemberAuditFindingKind : AuditFindingKindBase
{
    /// <summary>Initializes a new instance of the <see cref="UnimplementedMemberAuditFindingKind"/> class.</summary>
    public UnimplementedMemberAuditFindingKind() : base(3, "UnimplementedMember") { }
}
