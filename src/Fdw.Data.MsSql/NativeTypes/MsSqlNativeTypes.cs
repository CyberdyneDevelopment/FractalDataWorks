using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// TypeCollection of SQL Server's native data types — the vocabulary a SQL Server connection speaks.
/// </summary>
/// <remarks>
/// <para>
/// Closes on <see cref="IMsSqlDataType"/>, so <c>ByName</c> hands back a view carrying the facets SQL
/// Server actually has — length, precision, scale, unicode, fixed-versus-variable — and NOT the ones it
/// does not, such as a wire format. Every other vocabulary (JSON Schema, EDM, delimited) closes on its
/// own interface over the SAME <see cref="DataTypeOptionBase"/>, which is what lets one option class
/// serve all of them while each caller sees only what its vocabulary can express.
/// </para>
/// <para>
/// Source generator produces a static property per <c>[TypeOption]</c>, plus <c>All()</c>,
/// <c>ByName()</c>, <c>ById()</c> and a <c>NotFound</c> sentinel.
/// </para>
/// </remarks>
[TypeCollection(typeof(DataTypeOptionBase), typeof(IMsSqlDataType), typeof(MsSqlNativeTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class MsSqlNativeTypes : TypeCollectionBase<DataTypeOptionBase, IMsSqlDataType>
{
    // Source generator will create:
    //   - Static constructor
    //   - Static properties for each [TypeOption] native type
    //   - All() / ByName() / ById() / NotFound() methods
}
