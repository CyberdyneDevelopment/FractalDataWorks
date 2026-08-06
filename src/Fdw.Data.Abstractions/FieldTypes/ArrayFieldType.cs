using System;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Implementation of array/collection field types with recursive element support.
/// </summary>
public sealed class ArrayFieldType : IArrayFieldType
{
    /// <summary>
    /// Gets or initializes the element type.
    /// </summary>
    public required IFieldType ElementType { get; init; }

    /// <summary>
    /// Gets the type name as "Array&lt;ElementTypeName&gt;".
    /// </summary>
    public string TypeName => $"Array<{ElementType.TypeName}>";

    /// <summary>
    /// Gets the CLR array type.
    /// </summary>
    public Type ClrType => typeof(Array);

    /// <summary>
    /// Gets whether this is a nested type. Always true for arrays.
    /// </summary>
    public bool IsNested => true;
}
