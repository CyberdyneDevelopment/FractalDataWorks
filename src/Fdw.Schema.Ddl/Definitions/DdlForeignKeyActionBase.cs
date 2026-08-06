using Fdw.Collections;

namespace Fdw.Schema.Ddl.Definitions;

/// <summary>Base class for DDL foreign key actions.</summary>
public abstract class DdlForeignKeyActionBase : TypeOptionBase<int, DdlForeignKeyActionBase>, IDdlForeignKeyAction
{
    /// <summary>Initializes a new instance of <see cref="DdlForeignKeyActionBase"/>.</summary>
    protected DdlForeignKeyActionBase(int id, string name) : base(id, name) { }
}
