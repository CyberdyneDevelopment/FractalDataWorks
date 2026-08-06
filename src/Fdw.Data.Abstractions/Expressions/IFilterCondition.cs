namespace Fdw.Data.Abstractions;

/// <summary>
/// Interface for filter conditions.
/// </summary>
/// <remarks>
/// Implemented by FilterCondition record.
/// Enables mocking and abstraction in tests and dependency injection.
/// </remarks>
public interface IFilterCondition
{
    /// <summary>
    /// Gets the property name to filter on.
    /// </summary>
    string PropertyName { get; }

    /// <summary>
    /// Gets the filter operator.
    /// </summary>
    IFilterOperator Operator { get; }

    /// <summary>
    /// Gets the value to compare against (null for IS NULL / IS NOT NULL operators).
    /// </summary>
    object? Value { get; }
}
