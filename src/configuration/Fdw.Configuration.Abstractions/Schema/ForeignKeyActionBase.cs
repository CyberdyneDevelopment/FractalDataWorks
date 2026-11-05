using Fdw.Collections;

namespace Fdw.Configuration.Persistence.Schema;

/// <summary>
/// Base class for foreign key referential actions.
/// </summary>
public abstract class ForeignKeyActionBase : TypeOptionBase<ForeignKeyActionBase>, IForeignKeyAction
{
    /// <summary>
    /// Initializes a new instance of <see cref="ForeignKeyActionBase"/>.
    /// </summary>
    /// <param name="name">The action's name.</param>
    /// <param name="sqlKeyword">The action's SQL keyword, as it follows ON DELETE / ON UPDATE.</param>
    protected ForeignKeyActionBase(string name, string sqlKeyword) : base(name) => SqlKeyword = sqlKeyword;

    /// <inheritdoc />
    public string SqlKeyword { get; }
}
