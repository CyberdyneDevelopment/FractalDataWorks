using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Search;

[TypeCollection(typeof(SqlCommandTranslatorBase), typeof(ISqlCommandTranslator), typeof(SqlSearchTranslators))]
public abstract partial class SqlSearchTranslators
    : TypeCollectionBase<SqlCommandTranslatorBase, ISqlCommandTranslator>
{
}
