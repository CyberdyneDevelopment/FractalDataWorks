using System;
using Fdw.Protocols.Tds.Types;

namespace Fdw.Tds.Endpoints.Sql.Rows;

/// <summary>
/// A column a query can name: which source it came from, its name, and the type its source declares.
/// </summary>
public sealed class SqlBoundColumn
{
    /// <summary>Initializes a new instance of the <see cref="SqlBoundColumn"/> class.</summary>
    /// <param name="qualifier">The alias or object name a query qualifies it by.</param>
    /// <param name="schema">The schema of the object it came from.</param>
    /// <param name="name">The column's name.</param>
    /// <param name="type">The type its source declares, or null when it is known only from its values.</param>
    /// <param name="isNullable">Whether it may hold NULL.</param>
    public SqlBoundColumn(string qualifier, string schema, string name, TdsColumnType? type, bool isNullable)
    {
        Qualifier = qualifier ?? throw new ArgumentNullException(nameof(qualifier));
        Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type;
        IsNullable = isNullable;
    }

    /// <summary>Gets the alias or object name a query qualifies it by.</summary>
    public string Qualifier { get; }

    /// <summary>Gets the schema of the object it came from.</summary>
    public string Schema { get; }

    /// <summary>Gets the column's name.</summary>
    public string Name { get; }

    /// <summary>Gets the type its source declares, or null when it is known only from its values.</summary>
    public TdsColumnType? Type { get; }

    /// <summary>Gets a value indicating whether it may hold NULL.</summary>
    public bool IsNullable { get; }

    /// <summary>Gets a value indicating whether a reference of these parts names this column.</summary>
    /// <param name="schema">The schema written, or null.</param>
    /// <param name="qualifier">The qualifier written, or null.</param>
    /// <param name="name">The column name written.</param>
    /// <returns>True when every part written matches.</returns>
    public bool Matches(string? schema, string? qualifier, string name)
        => string.Equals(Name, name, StringComparison.OrdinalIgnoreCase)
           && (qualifier is null || string.Equals(Qualifier, qualifier, StringComparison.OrdinalIgnoreCase))
           && (schema is null || string.Equals(Schema, schema, StringComparison.OrdinalIgnoreCase));
}
