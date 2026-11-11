using Fdw.Collections;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>Base of every <see cref="AuditFindingKinds"/> option: what an audit finding is about.</summary>
public abstract class AuditFindingKindBase : TypeOptionBase<AuditFindingKindBase>, IAuditFindingKind
{
    /// <summary>Initializes a new instance of the <see cref="AuditFindingKindBase"/> class.</summary>
    /// <param name="id">The option's id; ids start at 1, since 0 is the collection's not-found value.</param>
    /// <param name="name">The option's name, as the results write it.</param>
    protected AuditFindingKindBase(int id, string name) : base(id, name) { }
}
