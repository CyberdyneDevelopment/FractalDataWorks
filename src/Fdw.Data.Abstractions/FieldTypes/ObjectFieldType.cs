using System;
using System.Collections.Generic;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Implementation of object/complex field types with nested fields.
/// </summary>
public sealed class ObjectFieldType : IObjectFieldType
{
    /// <summary>
    /// Gets or initializes the type name.
    /// </summary>
    public required string TypeName { get; init; }

    /// <summary>
    /// Gets or initializes the nested fields.
    /// </summary>
    public required IReadOnlyList<IField> Fields { get; init; }

    /// <summary>
    /// Gets or initializes the CLR type.
    /// </summary>
    public required Type ClrType { get; init; }

    /// <summary>
    /// Gets whether this is a nested type. Always true for objects.
    /// </summary>
    public bool IsNested => true;
}
