using Fdw.Collections;

namespace Fdw.Sql.Commands.Abstractions;

/// <summary>Base class for SQL command categories.</summary>
public abstract class SqlCommandCategoryBase : TypeOptionBase<SqlCommandCategoryBase>, ISqlCommandCategory
{
    /// <summary>Sentinel ctor.</summary>
    protected SqlCommandCategoryBase()
        : base(0, string.Empty, string.Empty, string.Empty, string.Empty, "SqlCommandCategory")
    { }

    /// <summary>Initializes a new category.</summary>
    protected SqlCommandCategoryBase(string name, string description)
        : base(name, name, name, description, "SqlCommandCategory")
    { }
}
