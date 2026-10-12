using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Analysis;

[TypeCollection(typeof(SqlCommandTranslatorBase), typeof(ISqlCommandTranslator), typeof(SqlAnalysisTranslators))]
public abstract partial class SqlAnalysisTranslators
    : TypeCollectionBase<SqlCommandTranslatorBase, ISqlCommandTranslator>
{
}
