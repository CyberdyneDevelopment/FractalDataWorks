namespace Fdw.Data.Abstractions;

/// <summary>
/// Interface for ordered fields.
/// </summary>
/// <remarks>
/// Implemented by OrderedField record.
/// Enables mocking and abstraction in tests and dependency injection.
/// </remarks>
public interface IOrderedField
{
    /// <summary>
    /// Gets the property name to order by.
    /// </summary>
    string PropertyName { get; }

    /// <summary>
    /// Gets the sort direction.
    /// </summary>
    ISortDirection Direction { get; }
}
