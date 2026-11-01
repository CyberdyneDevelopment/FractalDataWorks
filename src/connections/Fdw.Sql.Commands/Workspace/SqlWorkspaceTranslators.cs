using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Workspace;

[TypeCollection(typeof(SqlCommandTranslatorBase), typeof(ISqlCommandTranslator), typeof(SqlWorkspaceTranslators))]
public abstract partial class SqlWorkspaceTranslators
    : TypeCollectionBase<SqlCommandTranslatorBase, ISqlCommandTranslator>
{
}
