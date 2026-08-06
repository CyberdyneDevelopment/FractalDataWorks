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
    /// Formats the parameter placeholder for SQL.
    /// </summary>
    /// <param name="paramName">The parameter name.</param>
    /// <returns>The formatted parameter placeholder.</returns>
    string FormatSqlParameter(string paramName);

    /// <summary>
    /// Preprocesses a string value before it is added as a SQL parameter.
    /// Used to escape operator-specific metacharacters (e.g., LIKE metacharacters: %, _, [).
    /// </summary>
    /// <param name="value">The raw string value from the filter condition.</param>
    /// <returns>The preprocessed value ready for parameterization.</returns>
    string PreprocessSqlValue(string value);

    /// <summary>
    /// Formats the value for OData query strings.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The formatted OData value string.</returns>
    string FormatODataValue(object? value);
}
