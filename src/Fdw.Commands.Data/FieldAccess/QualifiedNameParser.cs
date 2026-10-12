using Fdw.Commands.Data.Abstractions.FieldAccess;

namespace Fdw.Commands.Data.FieldAccess;

/// <summary>
/// Default implementation of qualified name parsing.
/// Extracts the field name from a qualified name (e.g., "Customers.Id" -> "Id").
/// </summary>
public sealed class QualifiedNameParser : IQualifiedNameParser
{
    /// <inheritdoc />
    public string GetFieldName(string qualifiedName)
    {
        if (string.IsNullOrEmpty(qualifiedName))
        {
            return qualifiedName;
        }

        var dotIndex = qualifiedName.LastIndexOf('.');
        return dotIndex >= 0 ? qualifiedName[(dotIndex + 1)..] : qualifiedName;
    }
}
