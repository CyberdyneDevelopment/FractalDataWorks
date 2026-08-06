using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Sqlite.Translators;
using Microsoft.Data.Sqlite;

namespace Fdw.Services.Connections.Sqlite;

/// <summary>
/// TypeCollection of SQLite data command translators.
/// Discovered at compile-time via TypeCollection source generator.
/// </summary>
[TypeCollection(typeof(SqliteDataCommandTranslatorBase), typeof(IDataCommandTranslator<SqliteCommand>), typeof(SqliteDataCommandTranslators))]
[ExcludeFromCodeCoverage]
public abstract partial class SqliteDataCommandTranslators :
    TypeCollectionBase<SqliteDataCommandTranslatorBase, IDataCommandTranslator<SqliteCommand>>
{
}
