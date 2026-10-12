namespace Fdw.Commands.Data.Abstractions.FieldAccess;

/// <summary>
/// Extracts field values from records (handles various record types).
/// </summary>
public interface IFieldValueExtractor
{
    /// <summary>
    /// Gets the value of a field from a record.
    /// </summary>
    /// <param name="record">The record to extract from.</param>
    /// <param name="fieldName">The field name to extract.</param>
    /// <returns>The field value, or null if not found or record is null.</returns>
    object? GetValue(object? record, string fieldName);
}
