using Fdw.Data;

namespace Fdw.Commands.Data;

/// <summary>
/// Fluent builder for filter conditions with type-safe operator methods.
/// Created by QueryCommandBuilder.Where(c => c.Property) and returns to parent builder after operator selection.
/// </summary>
/// <typeparam name="T">The entity type being queried.</typeparam>
/// <typeparam name="TProperty">The property type being filtered on.</typeparam>
public sealed class FilterConditionBuilder<T, TProperty>
{
    private readonly QueryCommandBuilder<T> _parent;
    private readonly string _propertyName;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterConditionBuilder{T, TProperty}"/> class.
    /// </summary>
    /// <param name="parent">The parent query builder to return to after operator selection.</param>
    /// <param name="propertyName">The property name extracted from the expression.</param>
    internal FilterConditionBuilder(QueryCommandBuilder<T> parent, string propertyName)
    {
        _parent = parent;
        _propertyName = propertyName;
    }

    /// <summary>
    /// Filter where property equals the specified value.
    /// </summary>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The parent query builder for chaining.</returns>
    /// <example>
    /// <code>
    /// .Where(c => c.Status).Equal("Active")
    /// </code>
    /// </example>
    public QueryCommandBuilder<T> Equal(TProperty value)
        => _parent.Where(_propertyName, new EqualOperator(), value);

    /// <summary>
    /// Filter where property does not equal the specified value.
    /// </summary>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The parent query builder for chaining.</returns>
    public QueryCommandBuilder<T> NotEqual(TProperty value)
        => _parent.Where(_propertyName, new NotEqualOperator(), value);

    /// <summary>
    /// Filter where property is greater than the specified value.
    /// </summary>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The parent query builder for chaining.</returns>
    /// <example>
    /// <code>
    /// .Where(c => c.Age).GreaterThan(18)
    /// </code>
    /// </example>
    public QueryCommandBuilder<T> GreaterThan(TProperty value)
        => _parent.Where(_propertyName, new GreaterThanOperator(), value);

    /// <summary>
    /// Filter where property is greater than or equal to the specified value.
    /// </summary>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The parent query builder for chaining.</returns>
    public QueryCommandBuilder<T> GreaterThanOrEqual(TProperty value)
        => _parent.Where(_propertyName, new GreaterThanOrEqualOperator(), value);

    /// <summary>
    /// Filter where property is less than the specified value.
    /// </summary>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The parent query builder for chaining.</returns>
    public QueryCommandBuilder<T> LessThan(TProperty value)
        => _parent.Where(_propertyName, new LessThanOperator(), value);

    /// <summary>
    /// Filter where property is less than or equal to the specified value.
    /// </summary>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The parent query builder for chaining.</returns>
    public QueryCommandBuilder<T> LessThanOrEqual(TProperty value)
        => _parent.Where(_propertyName, new LessThanOrEqualOperator(), value);

    /// <summary>
    /// Filter where property contains the specified value (LIKE %value%).
    /// </summary>
    /// <param name="value">The value to search for.</param>
    /// <returns>The parent query builder for chaining.</returns>
    /// <example>
    /// <code>
    /// .Where(c => c.Name).Contains("Corp")
    /// </code>
    /// </example>
    public QueryCommandBuilder<T> Contains(TProperty value)
        => _parent.Where(_propertyName, new ContainsOperator(), value);

    /// <summary>
    /// Filter where property starts with the specified value (LIKE value%).
    /// </summary>
    /// <param name="value">The value to search for.</param>
    /// <returns>The parent query builder for chaining.</returns>
    public QueryCommandBuilder<T> StartsWith(TProperty value)
        => _parent.Where(_propertyName, new StartsWithOperator(), value);

    /// <summary>
    /// Filter where property ends with the specified value (LIKE %value).
    /// </summary>
    /// <param name="value">The value to search for.</param>
    /// <returns>The parent query builder for chaining.</returns>
    public QueryCommandBuilder<T> EndsWith(TProperty value)
        => _parent.Where(_propertyName, new EndsWithOperator(), value);

    /// <summary>
    /// Filter where property is in the specified collection of values.
    /// </summary>
    /// <param name="values">The collection of values to match against.</param>
    /// <returns>The parent query builder for chaining.</returns>
    /// <example>
    /// <code>
    /// .Where(c => c.Status).In(["Active", "Pending", "Review"])
    /// </code>
    /// </example>
    public QueryCommandBuilder<T> In(IEnumerable<TProperty> values)
        => _parent.Where(_propertyName, new InOperator(), values);

    /// <summary>
    /// Filter where property is in the specified values.
    /// </summary>
    /// <param name="values">The values to match against.</param>
    /// <returns>The parent query builder for chaining.</returns>
    /// <example>
    /// <code>
    /// .Where(c => c.Status).In("Active", "Pending", "Review")
    /// </code>
    /// </example>
    public QueryCommandBuilder<T> In(params TProperty[] values)
        => _parent.Where(_propertyName, new InOperator(), values);

    /// <summary>
    /// Filter where property is null.
    /// </summary>
    /// <returns>The parent query builder for chaining.</returns>
    public QueryCommandBuilder<T> IsNull()
        => _parent.Where(_propertyName, new IsNullOperator(), null);

    /// <summary>
    /// Filter where property is not null.
    /// </summary>
    /// <returns>The parent query builder for chaining.</returns>
    public QueryCommandBuilder<T> IsNotNull()
        => _parent.Where(_propertyName, new IsNotNullOperator(), null);
}