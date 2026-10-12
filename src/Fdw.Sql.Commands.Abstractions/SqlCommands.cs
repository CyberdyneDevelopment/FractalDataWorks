using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Sql.Commands.Abstractions;

/// <summary>Source-generated TypeCollection of every <see cref="ISqlCommand"/> across all Sql.Commands.* packages.</summary>
[TypeCollection(typeof(SqlCommandBase), typeof(ISqlCommand), typeof(SqlCommands))]
public abstract partial class SqlCommands
    : TypeCollectionBase<SqlCommandBase, ISqlCommand>
{
}
