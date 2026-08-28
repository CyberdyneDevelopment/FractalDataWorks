using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Data.DataStores.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Represents a path to a SQL Server database object.
/// Format: Database.Schema.Object (e.g., "Northwind.dbo.Customers")
/// </summary>
/// <remarks>
/// DatabasePath implements non-generic IDataNodePath because SQL paths can contain
/// multiple container types (Table, View, StoredProcedure with same name in different schemas).
/// Use Containers property to access typed SQL containers.
/// </remarks>
public sealed class DatabasePath : PathBase, IDataPath<IStorageContainer>, IDatabasePath
{
    private readonly List<IStorageContainer> _containers;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabasePath"/> class.
    /// </summary>
    /// <param name="database">The database name.</param>
    /// <param name="schema">The schema name (default: "dbo").</param>
    /// <param name="objectName">The object name (table, view, stored procedure).</param>
    /// <param name="containers">Optional containers at this path.</param>
    public DatabasePath(
        string? database,
        string schema,
        string objectName,
        IEnumerable<IStorageContainer>? containers = null)
        : base(1, "DatabasePath")
    {
        Database = database ?? string.Empty;
        Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        ObjectName = objectName ?? throw new ArgumentNullException(nameof(objectName));
        _containers = containers?.ToList() ?? new List<IStorageContainer>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabasePath"/> class with default schema "dbo".
    /// </summary>
    /// <param name="database">The database name.</param>
    /// <param name="objectName">The object name (table, view, stored procedure).</param>
    public DatabasePath(string database, string objectName)
        : this(database, "dbo", objectName)
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
    /// Gets the string representation in Database.Schema.Object format (or Schema.Object if no database).
    /// </summary>
    public override string PathValue => string.IsNullOrEmpty(Database)
        ? $"{Schema}.{ObjectName}"
        : $"{Database}.{Schema}.{ObjectName}";

    /// <summary>
    /// Gets the domain (Sql).
    /// </summary>
    public override string Domain => "Sql";

    /// <summary>
    /// Gets the quoted identifier format for T-SQL.
    /// Uses [Database].[Schema].[Object] when database is specified, otherwise [Schema].[Object].
    /// </summary>
    public string QuotedIdentifier => string.IsNullOrEmpty(Database)
        ? $"[{Schema}].[{ObjectName}]"
        : $"[{Database}].[{Schema}].[{ObjectName}]";

    /// <summary>
    /// Gets the schema.object format (without database).
    /// </summary>
    public string SchemaQualifiedName => $"[{Schema}].[{ObjectName}]";

    // IDatabasePath explicit implementation
    string? IDatabasePath.Database => Database;
    string? IDatabasePath.Schema => Schema;
    string IDatabasePath.ObjectName => ObjectName;
    ISqlDialect IDatabasePath.Dialect => TSqlDialect.Instance;

    // IDataNodePath implementation — using fully qualified type to resolve ambiguity with
    // Fdw.Data.Abstractions.IDataNodePath (Phase 1 DataNodes addition)
    string Fdw.Data.DataStores.Abstractions.IDataPath.Id => $"{Database}.{Schema}.{ObjectName}";
    string Fdw.Data.DataStores.Abstractions.IDataPath.Name => ObjectName;
    string Fdw.Data.DataStores.Abstractions.IDataPath.PathType => "DatabasePath";
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
