using Fdw.Collections;

namespace Fdw.Commands.Data.Ddl;

/// <summary>Base class for ALTER TABLE operation types.</summary>
public abstract class AlterTableOperationTypeBase : TypeOptionBase<int, AlterTableOperationTypeBase>, IAlterTableOperationType
{
    /// <summary>Initializes a new instance of <see cref="AlterTableOperationTypeBase"/>.</summary>
    protected AlterTableOperationTypeBase(int id, string name) : base(id, name) { }
}
