using System;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Base interface for all field types (simple, array, object).
/// Supports recursive nesting for JSON/XML hierarchical structures.
/// </summary>
public interface IFieldType
{
    /// <summary>
    /// The store's own name for the type, as the store reports it (e.g. <c>nvarchar</c>, <c>int</c>, <c>int4</c>, a JSON
    /// Schema <c>string</c>, or "Array&lt;Order&gt;" for a nested structure). It is what a data store container field
    /// records as its <c>DataType</c> and what DDL writes as a column's type; the portable type is
    /// <see cref="IField.ExplicitType"/> (FDW-1588).
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
