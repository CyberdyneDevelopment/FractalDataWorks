using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Sqlite.Authentication;

/// <summary>
/// TypeCollection of SQLite authentication methods. Each option parses the connection's authentication
/// KVP (from <c>conn.SqliteConnectionAuthentication</c>) and resolves the optional encryption-key
/// password. Selected by the <c>AuthenticationType</c> column on <c>conn.SqliteConnection</c>.
/// </summary>
/// <remarks>
/// Mirrors <c>MsSqlAuthenticationTypes</c>. The source generator populates ByName()/ById()/All()/
/// NotFound() at compile time.
/// </remarks>
[TypeCollection(
    typeof(SqliteAuthenticationConfiguration),
    typeof(SqliteAuthenticationConfiguration),
    typeof(SqliteAuthenticationTypes))]
public abstract partial class SqliteAuthenticationTypes
    : TypeCollectionBase<SqliteAuthenticationConfiguration, SqliteAuthenticationConfiguration>
{
}
