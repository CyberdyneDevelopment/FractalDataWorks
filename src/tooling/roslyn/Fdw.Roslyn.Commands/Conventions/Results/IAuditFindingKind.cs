using Fdw.Collections;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>One value of <see cref="AuditFindingKinds"/>: what an audit finding is about.</summary>
public interface IAuditFindingKind : ITypeOption<int, AuditFindingKindBase> { }
