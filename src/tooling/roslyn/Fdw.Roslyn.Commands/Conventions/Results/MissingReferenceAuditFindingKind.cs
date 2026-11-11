using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>The project lacks an assembly the guide requires.</summary>
[TypeOption(typeof(AuditFindingKinds), "MissingReference")]
[ExcludeFromCodeCoverage]
public sealed class MissingReferenceAuditFindingKind : AuditFindingKindBase
{
    /// <summary>Initializes a new instance of the <see cref="MissingReferenceAuditFindingKind"/> class.</summary>
    public MissingReferenceAuditFindingKind() : base(2, "MissingReference") { }
}
