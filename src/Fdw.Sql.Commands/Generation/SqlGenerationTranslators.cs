using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Generation;

[TypeCollection(typeof(SqlCommandTranslatorBase), typeof(ISqlCommandTranslator), typeof(SqlGenerationTranslators))]
public abstract partial class SqlGenerationTranslators
    : TypeCollectionBase<SqlCommandTranslatorBase, ISqlCommandTranslator>
{
}
