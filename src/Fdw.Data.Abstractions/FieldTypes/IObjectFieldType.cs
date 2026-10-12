using System.Collections.Generic;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Object/complex types with nested fields (can contain Simple, Array, or Object fields - recursive).
/// </summary>
public interface IObjectFieldType : IFieldType
{
    /// <summary>
    /// Fields within this object (can recursively contain arrays and objects).
    /// </summary>
    IReadOnlyList<IField> Fields { get; }
}
