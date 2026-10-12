using Fdw.Collections;

namespace Fdw.Tds.Endpoints.Sql.Globals;

/// <summary>
/// Base class for the T-SQL global variables.
/// </summary>
public abstract class SqlGlobalVariableBase : TypeOptionBase<SqlGlobalVariableBase>, ISqlGlobalVariable
{
    /// <summary>Initializes a new instance of the <see cref="SqlGlobalVariableBase"/> class.</summary>
    /// <param name="name">The option's name; must match its TypeOption attribute.</param>
    /// <param name="sqlName">The variable's T-SQL name, with its @@.</param>
    protected SqlGlobalVariableBase(string name, string sqlName)
        : base(name)
    {
        SqlName = sqlName;
    }

    /// <inheritdoc/>
    public string SqlName { get; }

    /// <inheritdoc/>
    public abstract object? Read(SqlExecutionContext context);
}
