namespace Fdw.Data.Abstractions;

/// <summary>
/// Base contract for all field bindings that resolve a <see cref="IDataField"/>'s runtime type
/// and data source from one or more nodes in the query graph.
/// </summary>
public interface IFieldBinding
{
    /// <summary>
    /// Gets the abstract data type that this binding resolves the owning field to at runtime.
    /// </summary>
    IDataType ResultType { get; }
}
