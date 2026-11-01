using Fdw.Collections;

namespace Fdw.Data;

/// <summary>
/// Interface for filter operators.
/// </summary>
/// <remarks>
/// <para>
/// Implemented by FilterOperatorBase and all concrete operators.
/// Enables mocking and abstraction in tests and dependency injection.
/// </para>
/// <para>
/// Extends ITypeOption to enable TypeCollection pattern with source generator discovery.
/// </para>
/// </remarks>
public interface IFilterOperator : ITypeOption<int, IFilterOperator>
{
    /// <summary>
    /// Gets the SQL representation of this operator.
    /// </summary>
    /// <value>The SQL operator string (e.g., "=", "&lt;&gt;", "LIKE", "IS NULL").</value>
    string SqlOperator { get; }

    /// <summary>
    /// Gets the OData representation of this operator.
    /// </summary>
    /// <value>The OData operator string (e.g., "eq", "ne", "contains").</value>
    string ODataOperator { get; }

    /// <summary>
    /// Gets a value indicating whether this operator requires a value parameter.
    /// </summary>
    bool RequiresValue { get; }

    /// <summary>
    /// Gets a value indicating whether this operator matches a pattern built around its value -- text that
    /// contains, starts with or ends with it -- rather than comparing the value whole.
    /// </summary>
    /// <remarks>
    /// The operator states its shape once, here, and each connection decides how to express it: a SQL
    /// dialect builds a LIKE pattern and says which characters it must escape, OData names a function,
    /// a source that cannot filter natively evaluates <c>Matches</c>. What the shape is belongs to the
    /// operator; how it is written belongs to the connection.
    /// </remarks>
    bool IsPatternMatch { get; }

    /// <summary>Gets a value indicating whether any text may precede the value in a match (Contains, EndsWith).</summary>
    bool MatchesAnythingBefore { get; }

    /// <summary>Gets a value indicating whether any text may follow the value in a match (Contains, StartsWith).</summary>
    bool MatchesAnythingAfter { get; }

    /// <summary>
    /// Formats the value for OData query strings.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The formatted OData value string.</returns>
    string FormatODataValue(object? value);
}
