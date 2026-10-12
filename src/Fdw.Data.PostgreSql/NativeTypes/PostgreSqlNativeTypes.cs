using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// TypeCollection of all PostgreSQL native data types, each mapping to an abstract
/// <see cref="Fdw.Data.Abstractions.IDataType"/> for portable DataSet field definitions.
/// </summary>
/// <remarks>
/// Source generator produces static properties for each <see cref="PostgreSqlNativeTypeBase"/>
/// decorated with <c>[TypeOption]</c>, plus <c>All()</c>, <c>ByName()</c>, <c>ById()</c>,
/// and a <c>NotFound</c> sentinel.
/// </remarks>
[TypeCollection(typeof(PostgreSqlNativeTypeBase), typeof(IPostgreSqlNativeType), typeof(PostgreSqlNativeTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class PostgreSqlNativeTypes : TypeCollectionBase<PostgreSqlNativeTypeBase, IPostgreSqlNativeType>
{
    // Source generator will create:
    //   - Static constructor
    //   - Static properties for each [TypeOption] native type
    //   - All() / ByName() / ById() / NotFound() methods
}
