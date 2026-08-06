using System;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Base interface for all field types (simple, array, object).
/// Supports recursive nesting for JSON/XML hierarchical structures.
/// </summary>
public interface IFieldType
{
    /// <summary>
    /// Type name (e.g., "Int32", "String", "Array&lt;Order&gt;").
    /// </summary>
    string TypeName { get; }

    /// <summary>
    /// CLR type if known (may be typeof(object) for dynamic types).
    /// </summary>
    Type ClrType { get; }

    /// <summary>
    /// Whether this is a nested type (array or object).
    /// </summary>
    bool IsNested { get; }
}
