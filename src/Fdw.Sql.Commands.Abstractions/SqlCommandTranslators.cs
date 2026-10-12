using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Sql.Commands.Abstractions;

/// <summary>Source-generated TypeCollection of every <see cref="ISqlCommandTranslator"/> across all Sql.Commands.* packages.</summary>
[TypeCollection(typeof(SqlCommandTranslatorBase), typeof(ISqlCommandTranslator), typeof(SqlCommandTranslators))]
public abstract partial class SqlCommandTranslators
    : TypeCollectionBase<SqlCommandTranslatorBase, ISqlCommandTranslator>
{
}
