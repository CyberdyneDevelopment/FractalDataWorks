using Fdw.Commands.Data.Abstractions;
using Fdw.Data;

namespace Fdw.Commands.Data;

/// <summary>
/// Extension methods providing shortcuts for common filter and aggregation operators.
/// Reduces boilerplate when building queries fluently.
/// </summary>
public static class FilterOperatorExtensions
{
    /// <summary>
    /// Add an equality WHERE condition (column = value).
    /// </summary>
    public static QueryCommandBuilder<T> WhereEqual<T>(
        this QueryCommandBuilder<T> builder, string property, object? value)
        => builder.Where(property, new EqualOperator(), value);

    /// <summary>
    /// Add a not-equal WHERE condition (column != value).
    /// </summary>
    public static QueryCommandBuilder<T> WhereNotEqual<T>(
        this QueryCommandBuilder<T> builder, string property, object? value)
        => builder.Where(property, new NotEqualOperator(), value);

    /// <summary>
    /// Add a greater-than WHERE condition (column > value).
    /// </summary>
    public static QueryCommandBuilder<T> WhereGreaterThan<T>(
        this QueryCommandBuilder<T> builder, string property, object? value)
        => builder.Where(property, new GreaterThanOperator(), value);

    /// <summary>
    /// Add a greater-than-or-equal WHERE condition (column >= value).
    /// </summary>
    public static QueryCommandBuilder<T> WhereGreaterThanOrEqual<T>(
        this QueryCommandBuilder<T> builder, string property, object? value)
        => builder.Where(property, new GreaterThanOrEqualOperator(), value);

    /// <summary>
    /// Add a less-than WHERE condition (column &lt; value).
    /// </summary>
    public static QueryCommandBuilder<T> WhereLessThan<T>(
        this QueryCommandBuilder<T> builder, string property, object? value)
        => builder.Where(property, new LessThanOperator(), value);

    /// <summary>
    /// Add a less-than-or-equal WHERE condition (column &lt;= value).
    /// </summary>
    public static QueryCommandBuilder<T> WhereLessThanOrEqual<T>(
        this QueryCommandBuilder<T> builder, string property, object? value)
        => builder.Where(property, new LessThanOrEqualOperator(), value);

    /// <summary>
    /// Add a contains WHERE condition (column LIKE %value%).
    /// </summary>
    public static QueryCommandBuilder<T> WhereContains<T>(
        this QueryCommandBuilder<T> builder, string property, object? value)
        => builder.Where(property, new ContainsOperator(), value);

    /// <summary>
    /// Add a starts-with WHERE condition (column LIKE value%).
    /// </summary>
    public static QueryCommandBuilder<T> WhereStartsWith<T>(
        this QueryCommandBuilder<T> builder, string property, object? value)
        => builder.Where(property, new StartsWithOperator(), value);

    /// <summary>
    /// Add an ends-with WHERE condition (column LIKE %value).
    /// </summary>
    public static QueryCommandBuilder<T> WhereEndsWith<T>(
        this QueryCommandBuilder<T> builder, string property, object? value)
        => builder.Where(property, new EndsWithOperator(), value);

    /// <summary>
    /// Add an IN WHERE condition (column IN (values)).
    /// </summary>
    public static QueryCommandBuilder<T> WhereIn<T>(
        this QueryCommandBuilder<T> builder, string property, object? value)
        => builder.Where(property, new InOperator(), value);

    // Additional operators (NotIn, Between, Aggregations, etc) will be added
    // when corresponding operator types are implemented in Commands.Data.Abstractions
}
