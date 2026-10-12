using System;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Implementation of simple primitive field types.
/// </summary>
public sealed class SimpleFieldType : ISimpleFieldType
{
    /// <summary>
    /// Gets or initializes the type name.
    /// </summary>
    public required string TypeName { get; init; }

    /// <summary>
    /// Gets or initializes the CLR type.
    /// </summary>
    public required Type ClrType { get; init; }

    /// <summary>
    /// Gets whether this is a nested type. Always false for simple types.
    /// </summary>
    public bool IsNested => false;
}
