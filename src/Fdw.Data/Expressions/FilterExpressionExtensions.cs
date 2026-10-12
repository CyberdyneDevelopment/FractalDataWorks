using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;

namespace Fdw.Data;

/// <summary>
/// Fluent factory methods for constructing <see cref="FilterExpression"/> instances
/// from key metadata and logical operators.
/// </summary>
/// <remarks>
/// <para>
/// These methods provide an ergonomic API for consumer code that needs to build filter expressions
/// without going through a key. The resulting expressions are fully interchangeable with
/// manually constructed <see cref="FilterExpression"/> objects.
/// </para>
/// <para>
/// Usage with a named field:
/// <code>
/// var expr = FilterExpression.Equal("Name", "Acme");
/// var combined = FilterExpression.And(
///     FilterExpression.Equal("Status", "Active"),
///     FilterExpression.Equal("Region", "US"));
/// </code>
/// </para>
/// <para>
/// Usage with an <see cref="IDataField"/> from container key metadata:
/// <code>
/// var pk = container.Keys.First(k => k.KeyType == KeyTypes.Primary);
/// var condition = FilterExpression.Equal(pk.KeyFields[0].LocalField, id);
/// </code>
/// </para>
/// </remarks>
public static class FilterExpressionExtensions
{
    // =========================================================================
    // Static factory methods on FilterExpression
    // =========================================================================

    /// <summary>
    /// Builds a <see cref="FilterExpression"/> that tests whether <paramref name="fieldName"/>
    /// equals <paramref name="value"/>.
    /// </summary>
    /// <param name="fieldName">The property or column name to filter on.</param>
    /// <param name="value">The equality target value.</param>
    /// <returns>A <see cref="FilterExpression"/> with an equality root condition.</returns>
    public static FilterExpression Equal(string fieldName, object value)
    {
        return new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = fieldName,
                Operator = new EqualOperator(),
                Value = value
            }
        };
    }

    /// <summary>
    /// Builds a <see cref="FilterExpression"/> that tests whether the field described by
    /// <paramref name="field"/> equals <paramref name="value"/>.
    /// </summary>
    /// <param name="field">The data field whose <see cref="IDataNode.Name"/> is used.</param>
    /// <param name="value">The equality target value.</param>
    /// <returns>A <see cref="FilterExpression"/> with an equality root condition.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="field"/> is <see langword="null"/>.
    /// </exception>
    public static FilterExpression Equal(IDataField field, object value)
    {
        if (field is null) throw new ArgumentNullException(nameof(field));
        return Equal(field.Name, value);
    }

    /// <summary>
    /// Combines two or more <see cref="FilterExpression"/> instances with logical AND.
    /// </summary>
    /// <param name="predicates">The expressions to combine. Must contain at least one element.</param>
    /// <returns>
    /// A single <see cref="FilterExpression"/> whose root is a <see cref="FilterGroup"/>
    /// with <see cref="LogicalOperator.And"/>, or the single expression when only one
    /// predicate is supplied.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="predicates"/> is empty.
    /// </exception>
    public static FilterExpression And(params FilterExpression[] predicates)
    {
        if (predicates is null || predicates.Length == 0)
            throw new ArgumentException("At least one predicate is required.", nameof(predicates));

        if (predicates.Length == 1)
            return predicates[0];

        return new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.And,
                Nodes = predicates
                    .Select(p => p.Root)
                    .OfType<IFilterNode>()
                    .ToArray()
            }
        };
    }

    /// <summary>
    /// Combines two or more <see cref="FilterExpression"/> instances with logical OR.
    /// </summary>
    /// <param name="predicates">The expressions to combine. Must contain at least one element.</param>
    /// <returns>
    /// A single <see cref="FilterExpression"/> whose root is a <see cref="FilterGroup"/>
    /// with <see cref="LogicalOperator.Or"/>, or the single expression when only one
    /// predicate is supplied.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="predicates"/> is empty.
    /// </exception>
    public static FilterExpression Or(params FilterExpression[] predicates)
    {
        if (predicates is null || predicates.Length == 0)
            throw new ArgumentException("At least one predicate is required.", nameof(predicates));

        if (predicates.Length == 1)
            return predicates[0];

        return new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.Or,
                Nodes = predicates
                    .Select(p => p.Root)
                    .OfType<IFilterNode>()
                    .ToArray()
            }
        };
    }

    /// <summary>
    /// Combines this <see cref="FilterExpression"/> with <paramref name="other"/> using
    /// logical AND.
    /// </summary>
    /// <param name="left">The left-hand predicate.</param>
    /// <param name="other">The right-hand predicate to combine with.</param>
    /// <returns>
    /// A new <see cref="FilterExpression"/> that is the AND of the two predicates.
    /// </returns>
    public static FilterExpression AndAlso(this FilterExpression left, FilterExpression other)
    {
        return And(left, other);
    }

    /// <summary>
    /// Combines this <see cref="FilterExpression"/> with <paramref name="other"/> using
    /// logical OR.
    /// </summary>
    /// <param name="left">The left-hand predicate.</param>
    /// <param name="other">The right-hand predicate to combine with.</param>
    /// <returns>
    /// A new <see cref="FilterExpression"/> that is the OR of the two predicates.
    /// </returns>
    public static FilterExpression OrElse(this FilterExpression left, FilterExpression other)
    {
        return Or(left, other);
    }
}
