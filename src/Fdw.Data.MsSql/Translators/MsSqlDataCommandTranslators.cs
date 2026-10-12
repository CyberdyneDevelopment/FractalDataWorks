using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.MsSql.Translators;
using Microsoft.Data.SqlClient;

namespace Fdw.Data.MsSql;

/// <summary>
/// TypeCollection of MS SQL Server (T-SQL) data command translators.
/// Discovered at compile-time via TypeCollection source generator.
/// </summary>
/// <remarks>
/// <para>
/// This collection provides all T-SQL-specific translators for converting universal
/// IDataCommand objects into T-SQL connection commands.
/// </para>
/// <para>
/// Source generator creates static properties for each [TypeOption] translator:
/// <list type="bullet">
/// <item>MsSqlDataCommandTranslators.MsSqlQuery - SELECT statement translator</item>
/// <item>MsSqlDataCommandTranslators.MsSqlInsert - INSERT statement translator</item>
/// <item>MsSqlDataCommandTranslators.MsSqlUpdate - UPDATE statement translator</item>
/// <item>MsSqlDataCommandTranslators.MsSqlDelete - DELETE statement translator</item>
/// </list>
/// </para>
/// <para>
/// These translators are registered at connection type registration time and made
/// available to the DataCommandTranslators collection.
/// </para>
/// <para>
/// Oracle and PostgreSQL would have separate OracleDataCommandTranslators and
/// PostgreSqlDataCommandTranslators collections with their own dialect-specific translators.
/// </para>
/// </remarks>
[TypeCollection(typeof(MsSqlDataCommandTranslatorBase), typeof(IDataCommandTranslator<SqlCommand>), typeof(MsSqlDataCommandTranslators))]
[ExcludeFromCodeCoverage]
public abstract partial class MsSqlDataCommandTranslators :
    TypeCollectionBase<MsSqlDataCommandTranslatorBase, IDataCommandTranslator<SqlCommand>>
{
    // Source generator creates:
    // - Static constructor
    // - Static properties: Query, Insert, Update, Delete, BulkInsert, BatchInsert, CompoundQuery
    // - public static IReadOnlyList<IDataCommandTranslator> All()
    // - public static IDataCommandTranslator ByName(string name)
    // - public static IDataCommandTranslator ById(int id)

}
