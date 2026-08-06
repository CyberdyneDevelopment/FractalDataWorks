using Fdw.Collections;

namespace Fdw.Configuration.Persistence.Schema;

/// <summary>
/// Base class for foreign key referential actions.
/// </summary>
public abstract class ForeignKeyActionBase : TypeOptionBase<int, ForeignKeyActionBase>, IForeignKeyAction
{
    /// <summary>
    /// Initializes a new instance of <see cref="ForeignKeyActionBase"/>.
    /// </summary>
    protected ForeignKeyActionBase(int id, string name) : base(id, name) { }
}
