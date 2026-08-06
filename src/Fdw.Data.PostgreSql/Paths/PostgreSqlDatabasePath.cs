using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Data.DataStores.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Represents a path to a PostgreSQL database object.
/// Format: Schema.Object (e.g., "public.customers")
/// </summary>
/// <remarks>
/// PostgreSQL uses double-quote quoting for identifiers: "schema"."table".
/// PostgreSQL does not typically use three-part names (database.schema.table)
/// since cross-database queries are not directly supported.
/// </remarks>
public sealed class PostgreSqlDatabasePath : PathBase, IDataPath<IStorageContainer>, IDatabasePath
{
    private readonly List<IStorageContainer> _containers;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlDatabasePath"/> class.
    /// </summary>
    /// <param name="database">The database name (optional, for metadata only).</param>
    /// <param name="schema">The schema name (default: "public").</param>
    /// <param name="objectName">The object name (table, view, function).</param>
    /// <param name="containers">Optional containers at this path.</param>
    public PostgreSqlDatabasePath(
        string? database,
        string schema,
        string objectName,
        IEnumerable<IStorageContainer>? containers = null)
        : base(1, "PostgreSqlDatabasePath")
    {
        Database = database ?? string.Empty;
        Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        ObjectName = objectName ?? throw new ArgumentNullException(nameof(objectName));
        _containers = containers?.ToList() ?? new List<IStorageContainer>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlDatabasePath"/> class with default schema "public".
    /// </summary>
    /// <param name="database">The database name.</param>
    /// <param name="objectName">The object name (table, view, function).</param>
    public PostgreSqlDatabasePath(string database, string objectName)
        : this(database, "public", objectName)
    {
    }

    /// <summary>
    /// Gets the database name.
    /// </summary>
    public string Database { get; }

    /// <summary>
    /// Gets the schema name.
    /// </summary>
    public string Schema { get; }

    /// <summary>
    /// Gets the object name.
    /// </summary>
    public string ObjectName { get; }

    /// <summary>
    /// Gets the string representation in Schema.Object format.
    /// </summary>
    public override string PathValue => $"{Schema}.{ObjectName}";

    /// <summary>
    /// Gets the domain (Sql).
    /// </summary>
    public override string Domain => "Sql";

    /// <summary>
    /// Gets the quoted identifier format for PostgreSQL.
    /// Uses "Schema"."Object" with double-quote quoting.
    /// </summary>
    public string QuotedIdentifier => $"\"{Schema}\".\"{ObjectName}\"";

    /// <summary>
    /// Gets the schema.object format with quoting.
    /// </summary>
    public string SchemaQualifiedName => $"\"{Schema}\".\"{ObjectName}\"";

    // Why: IDatabasePath uses nullable string? for Database and Schema so that schemaless
    // dialects (e.g., SQLite) can return null. PostgreSqlDatabasePath stores non-nullable
    // strings, so explicit interface implementations bridge the nullability mismatch.
    string? IDatabasePath.Database => Database;
    string? IDatabasePath.Schema => Schema;
    string IDatabasePath.ObjectName => ObjectName;
    ISqlDialect IDatabasePath.Dialect => PlPgSqlDialect.Instance;

    // IDataPath implementation — using fully qualified type to resolve ambiguity with
    // Fdw.Data.Abstractions.IDataPath (Phase 1 DataNodes addition)
    string Fdw.Data.DataStores.Abstractions.IDataPath.Id => $"{Database}.{Schema}.{ObjectName}";
    string Fdw.Data.DataStores.Abstractions.IDataPath.Name => ObjectName;
    string Fdw.Data.DataStores.Abstractions.IDataPath.PathType => "PostgreSqlDatabasePath";
    string Fdw.Data.DataStores.Abstractions.IDataPath.FullPath => PathValue;
    IReadOnlyList<string> Fdw.Data.DataStores.Abstractions.IDataPath.Segments => new[] { Database, Schema, ObjectName };
    IReadOnlyDictionary<string, PathParameter> Fdw.Data.DataStores.Abstractions.IDataPath.Parameters => new Dictionary<string, PathParameter>(StringComparer.Ordinal);
    IReadOnlyDictionary<string, object> Fdw.Data.DataStores.Abstractions.IDataPath.Metadata => new Dictionary<string, object>(StringComparer.Ordinal);
    bool Fdw.Data.DataStores.Abstractions.IDataPath.RequiresParameters => false;

    /// <inheritdoc/>
    public IReadOnlyList<IStorageContainer> Containers => _containers;

    /// <inheritdoc/>
    public IStorageContainer? GetContainer(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return _containers.FirstOrDefault(c =>
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public bool ContainsContainer(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        return _containers.Any(c =>
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    Fdw.Data.DataStores.Abstractions.IDataPath Fdw.Data.DataStores.Abstractions.IDataPath.ResolveParameters(IDictionary<string, object> parameters) => this;

    IGenericResult Fdw.Data.DataStores.Abstractions.IDataPath.ValidateParameters(IDictionary<string, object> parameters) =>
        GenericResult.Success();

    Fdw.Data.DataStores.Abstractions.IDataPath? Fdw.Data.DataStores.Abstractions.IDataPath.GetParent() => null;

    IEnumerable<Fdw.Data.DataStores.Abstractions.IDataPath> Fdw.Data.DataStores.Abstractions.IDataPath.GetChildren() => Enumerable.Empty<Fdw.Data.DataStores.Abstractions.IDataPath>();

    Fdw.Data.DataStores.Abstractions.IDataPath Fdw.Data.DataStores.Abstractions.IDataPath.Combine(string relativePath) => this;
}
