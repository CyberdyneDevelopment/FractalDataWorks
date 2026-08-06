using Fdw.Collections;

namespace Fdw.Commands.Data.Ddl;

/// <summary>
/// Base class for foreign key referential action types.
/// </summary>
public abstract class ForeignKeyActionBase : TypeOptionBase<int, ForeignKeyActionBase>, IForeignKeyAction
{
    /// <summary>
    /// Initializes a new instance of <see cref="ForeignKeyActionBase"/>.
    /// </summary>
    protected ForeignKeyActionBase(int id, string name) : base(id, name) { }
}
