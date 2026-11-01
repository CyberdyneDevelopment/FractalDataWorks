namespace Fdw.Data.Abstractions;

/// <summary>
/// Array/collection types with element type that can be Simple, Array, or Object (recursive).
/// </summary>
public interface IArrayFieldType : IFieldType
{
    /// <summary>
    /// Type of elements in the array (can be ISimpleFieldType, IArrayFieldType, or IObjectFieldType).
    /// </summary>
    IFieldType ElementType { get; }
}
