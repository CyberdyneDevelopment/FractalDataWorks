using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Sql.Commands.Abstractions;

/// <summary>Source-generated TypeCollection of <see cref="ISqlCommandCategory"/>.</summary>
[TypeCollection(typeof(SqlCommandCategoryBase), typeof(ISqlCommandCategory), typeof(SqlCommandCategories))]
public abstract partial class SqlCommandCategories
    : TypeCollectionBase<SqlCommandCategoryBase, ISqlCommandCategory>
{
}
