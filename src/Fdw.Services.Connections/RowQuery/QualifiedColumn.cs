using System;

namespace Fdw.Services.Connections.RowQuery;

/// <summary>
/// Splits a filter condition's <c>PropertyName</c> into its optional table qualifier and bare column,
/// matching the SQL translators' <c>BuildWhereClause</c> qualification convention: a BARE name targets
/// the queried (child) container; a DOTTED <c>"{ContainerName}.Column"</c> name targets the joined
/// (parent) container.
/// </summary>
/// <remarks>
/// Why one splitter: the validator (which resolves the column against a container's declared fields) and
/// the matcher (which resolves it against a row) must agree on which container a property name names.
/// Two copies of this rule would drift, and a drift here silently reads the wrong row.
/// </remarks>
internal static class QualifiedColumn
{
    /// <summary>
    /// Splits <paramref name="propertyName"/> into its qualifier (null when bare) and bare column name.
    /// </summary>
    internal static (string? Qualifier, string Column) Split(string propertyName)
    {
        var dot = propertyName.IndexOf('.', StringComparison.Ordinal);
        return dot < 0
            ? (null, propertyName)
            : (propertyName.Substring(0, dot), propertyName.Substring(dot + 1));
    }
}
