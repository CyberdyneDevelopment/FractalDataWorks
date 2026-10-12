namespace Fdw.Commands.Data.Abstractions.FieldAccess;

/// <summary>
/// Parses qualified field names (e.g., "Customers.Id" -> "Id").
/// </summary>
public interface IQualifiedNameParser
{
    /// <summary>
    /// Extracts the field name from a qualified name.
    /// </summary>
    /// <param name="qualifiedName">The qualified name (e.g., "Customers.Id").</param>
    /// <returns>The field name (e.g., "Id").</returns>
    string GetFieldName(string qualifiedName);
}
