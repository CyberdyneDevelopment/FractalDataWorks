using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>TypeCollection of what an audit finding is about.</summary>
[TypeCollection(typeof(AuditFindingKindBase), typeof(IAuditFindingKind), typeof(AuditFindingKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class AuditFindingKinds : TypeCollectionBase<AuditFindingKindBase, IAuditFindingKind> { }
