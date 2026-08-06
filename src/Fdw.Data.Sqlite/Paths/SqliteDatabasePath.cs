using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Data.DataStores.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Data.Sqlite;

/// <summary>
/// Represents a path to a SQLite database object.
/// Format: Object only (e.g., "customers") — SQLite has no schema namespace.
/// </summary>
/// <remarks>
/// SQLite uses double-quote quoting for identifiers: "table".
/// Because SQLite does not support schema namespaces, <see cref="Schema"/> is always null
/// and <see cref="IDatabasePath"/> reports no schema namespace support.
/// </remarks>
public sealed class SqliteDatabasePath : PathBase, IDataPath<IStorageContainer>, IDatabasePath
{
    private readonly List<IStorageContainer> _containers;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteDatabasePath"/> class.
    /// </summary>
    /// <param name="objectName">The object name (table or view).</param>
    /// <param name="containers">Optional containers at this path.</param>
    public SqliteDatabasePath(
        string objectName,
        IEnumerable<IStorageContainer>? containers = null)
        : base(1, "SqliteDatabasePath")
    {
        ObjectName = objectName ?? throw new ArgumentNullException(nameof(objectName));
        _containers = containers?.ToList() ?? new List<IStorageContainer>();
    }

    /// <summary>
    /// Gets the object name (table or view).
    /// </summary>
    public string ObjectName { get; }

    /// <summary>
    /// Gets the string representation (just the object name for SQLite).
    /// </summary>
    public override string PathValue => ObjectName;

    /// <summary>
    /// Gets the domain (Sql).
    /// </summary>
    public override string Domain => "Sql";

    /// <summary>
    /// Gets the quoted identifier for SQLite: <c>"ObjectName"</c>.
    /// </summary>
    public string QuotedIdentifier => $"\"{ObjectName}\"";

    // IDatabasePath explicit implementation
    // Why: SQLite has no database segment in the path — single file = one database.
    string? IDatabasePath.Database => null;
    // Why: SQLite has no schema namespace; Schema is always null per the schemaless-dialect rule.
    string? IDatabasePath.Schema => null;
    string IDatabasePath.ObjectName => ObjectName;
    // Why: dialect is a compile-time fact for this class — SQLite paths always use the SQLite dialect.
    ISqlDialect IDatabasePath.Dialect => SqliteDialect.Instance;

    // IDataPath implementation — using fully qualified type to resolve ambiguity
    string Fdw.Data.DataStores.Abstractions.IDataPath.Id => ObjectName;
    string Fdw.Data.DataStores.Abstractions.IDataPath.Name => ObjectName;
    string Fdw.Data.DataStores.Abstractions.IDataPath.PathType => "SqliteDatabasePath";
    string Fdw.Data.DataStores.Abstractions.IDataPath.FullPath => PathValue;
    IReadOnlyList<string> Fdw.Data.DataStores.Abstractions.IDataPath.Segments => new[] { ObjectName };
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
