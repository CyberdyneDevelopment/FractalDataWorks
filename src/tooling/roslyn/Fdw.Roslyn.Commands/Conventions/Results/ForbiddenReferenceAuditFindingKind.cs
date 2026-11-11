using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>The project references an assembly the guide forbids.</summary>
[TypeOption(typeof(AuditFindingKinds), "ForbiddenReference")]
[ExcludeFromCodeCoverage]
public sealed class ForbiddenReferenceAuditFindingKind : AuditFindingKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ForbiddenReferenceAuditFindingKind"/> class.</summary>
    public ForbiddenReferenceAuditFindingKind() : base(1, "ForbiddenReference") { }
}
