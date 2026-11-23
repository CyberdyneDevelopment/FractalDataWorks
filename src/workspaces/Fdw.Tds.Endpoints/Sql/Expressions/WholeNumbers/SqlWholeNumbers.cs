using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Expressions;

/// <summary>The .NET types a T-SQL whole number is held in (bigint, int, smallint, tinyint), found by <c>ByClrType</c>.</summary>
/// <remarks>A type no option holds is a value ~ does not apply to; <see cref="SqlArithmetic"/> refuses it as incompatible.</remarks>
[TypeCollection(typeof(SqlWholeNumberBase), typeof(ISqlWholeNumber), typeof(SqlWholeNumbers))]
[ExcludeFromCodeCoverage]
public abstract partial class SqlWholeNumbers : TypeCollectionBase<SqlWholeNumberBase, ISqlWholeNumber>
{
}
