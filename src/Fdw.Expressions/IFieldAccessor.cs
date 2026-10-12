using Fdw.Data.DataContainers.Abstractions;

namespace Fdw.Expressions;

/// <summary>
/// Provides efficient compiled access to a specific field in a data row.
/// </summary>
/// <typeparam name="TValue">The type of the field value.</typeparam>
public interface IFieldAccessor<TValue>
{
    /// <summary>
    /// Gets the field name this accessor retrieves.
    /// </summary>
    string FieldName { get; }

    /// <summary>
    /// Gets the field ordinal (for performance tracking).
    /// </summary>
    int Ordinal { get; }

    /// <summary>
    /// Gets the value from a row.
    /// </summary>
    /// <param name="row">The row to extract value from.</param>
    /// <returns>The field value.</returns>
    /// <remarks>
    /// This is a compiled expression, not a method call.
    /// Performance is equivalent to direct array access.
    /// </remarks>
    TValue GetValue(IDataRow row);

    /// <summary>
    /// Tries to get the value, returning default if null or conversion fails.
    /// </summary>
    /// <param name="row">The row to extract value from.</param>
    /// <param name="value">The output value if successful.</param>
    /// <returns>True if value was retrieved; false otherwise.</returns>
    bool TryGetValue(IDataRow row, out TValue? value);
}