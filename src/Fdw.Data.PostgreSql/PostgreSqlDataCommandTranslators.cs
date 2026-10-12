using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.PostgreSql.Translators;
using Npgsql;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// TypeCollection of PostgreSQL data command translators.
/// Discovered at compile-time via TypeCollection source generator.
/// </summary>
/// <remarks>
/// <para>
/// This collection provides all PostgreSQL-specific translators for converting universal
/// IDataCommand objects into PostgreSQL connection commands.
/// </para>
/// <para>
/// Source generator creates static properties for each [TypeOption] translator:
/// <list type="bullet">
/// <item>PostgreSqlDataCommandTranslators.Query - SELECT statement translator</item>
/// <item>PostgreSqlDataCommandTranslators.Insert - INSERT statement translator</item>
/// <item>PostgreSqlDataCommandTranslators.Update - UPDATE statement translator</item>
/// <item>PostgreSqlDataCommandTranslators.Delete - DELETE statement translator</item>
/// <item>PostgreSqlDataCommandTranslators.Find - LIKE/ILIKE search translator</item>
/// <item>PostgreSqlDataCommandTranslators.BulkInsert - COPY FROM STDIN translator</item>
/// <item>PostgreSqlDataCommandTranslators.BatchInsert - Multi-row VALUES translator</item>
/// <item>PostgreSqlDataCommandTranslators.CompoundQuery - JOIN query translator</item>
/// </list>
/// </para>
/// </remarks>
[TypeCollection(typeof(PostgreSqlDataCommandTranslatorBase), typeof(IDataCommandTranslator<NpgsqlCommand>), typeof(PostgreSqlDataCommandTranslators))]
[ExcludeFromCodeCoverage]
public abstract partial class PostgreSqlDataCommandTranslators :
    TypeCollectionBase<PostgreSqlDataCommandTranslatorBase, IDataCommandTranslator<NpgsqlCommand>>
{
    // Source generator creates:
    // - Static constructor
    // - Static properties: Query, Insert, Update, Delete, Find, BulkInsert, BatchInsert, CompoundQuery
    // - public static IReadOnlyList<IDataCommandTranslator<NpgsqlCommand>> All()
    // - public static IDataCommandTranslator<NpgsqlCommand> ByName(string name)
    // - public static IDataCommandTranslator<NpgsqlCommand> ById(int id)
}
