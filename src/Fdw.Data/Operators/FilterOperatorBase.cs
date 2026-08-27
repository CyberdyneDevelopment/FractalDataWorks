using Fdw.Collections;
using Fdw.Data.Abstractions;

namespace Fdw.Data;

/// <summary>
/// Base class for filter operators.
/// Replaces FilterOperator enum to add behavior and eliminate switch statements.
/// </summary>
/// <remarks>
/// <para>
/// Each operator knows its own SQL and OData representations, eliminating the need for
/// switch statements when translating commands to different domains.
/// </para>
/// <para>
/// Properties are set in constructor so TypeCollection source generator can read them
/// without instantiation.
/// </para>
/// </remarks>
public abstract class FilterOperatorBase : TypeOptionBase<int, FilterOperatorBase>, IFilterOperator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FilterOperatorBase"/> class.
    /// </summary>
    /// <param name="id">Unique identifier for this operator.</param>
    /// <param name="name">Name of the operator (must match TypeOption attribute).</param>
    /// <param name="sqlOperator">SQL representation (e.g., "=", "&lt;&gt;", "LIKE").</param>
    /// <param name="odataOperator">OData representation (e.g., "eq", "ne", "contains").</param>
    /// <param name="requiresValue">Whether this operator requires a value parameter.</param>
    protected FilterOperatorBase(int id, string name, string sqlOperator, string odataOperator, bool requiresValue)
        : base(id, name)
    {
        SqlOperator = sqlOperator;
        ODataOperator = odataOperator;
        RequiresValue = requiresValue;
    }

    /// <summary>
    /// Gets the SQL representation of this operator.
    /// No switch statements needed - direct property access!
    /// </summary>
    /// <value>The SQL operator string (e.g., "=", "&lt;&gt;", "LIKE", "IS NULL").</value>
    public string SqlOperator { get; }

    /// <summary>
    /// Gets the OData representation of this operator.
    /// No switch statements needed - direct property access!
    /// </summary>
    /// <value>The OData operator string (e.g., "eq", "ne", "contains").</value>
    public string ODataOperator { get; }

    /// <summary>
    /// Gets a value indicating whether this operator requires a value parameter.
    /// IS NULL and IS NOT NULL don't need values.
    /// </summary>
    public bool RequiresValue { get; }

    /// <summary>
    /// Formats the parameter placeholder for SQL.
    /// Subclasses can override for special behavior (e.g., LIKE wildcards).
    /// </summary>
    /// <param name="paramName">The parameter name.</param>
    /// <returns>The formatted parameter placeholder.</returns>
    public virtual string FormatSqlParameter(string paramName) => $"@{paramName}";

    /// <summary>
    /// Preprocesses a string value before it is added as a SQL parameter.
    /// Override in subclasses to escape operator-specific metacharacters (e.g., LIKE metacharacters).
    /// </summary>
    /// <param name="value">The raw string value from the filter condition.</param>
    /// <returns>The preprocessed value ready for parameterization.</returns>
    public virtual string PreprocessSqlValue(string value) => value;

    /// <summary>
    /// Formats the value for OData query strings.
    /// Subclasses must implement this to handle type-specific formatting.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The formatted OData value string.</returns>
    public abstract string FormatODataValue(object? value);

    /// <summary>Whether <paramref name="left"/> satisfies this operator against <paramref name="right"/>.</summary>
    /// <remarks>
    /// Why the operator evaluates rather than a switch somewhere else: the operator IS the
    /// comparison, and a source that cannot express the filter natively has to apply the same
    /// comparison in memory. One definition, two places it can run — otherwise the CSV path and the
    /// SQL path drift and the same filter means two things.
    /// </remarks>
    /// <param name="left">The value from the row.</param>
    /// <param name="right">The value from the filter.</param>
    /// <returns><see langword="true"/> when the row value satisfies this operator.</returns>
    public abstract bool Matches(object? left, object? right);
}
