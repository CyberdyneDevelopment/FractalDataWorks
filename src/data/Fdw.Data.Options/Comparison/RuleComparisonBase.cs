using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Collections;
using Fdw.Data.Abstractions;
using Fdw.Data.Options.Logging;
using Fdw.Data.Options.Results;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.Options;

/// <summary>
/// The base of the comparison kinds (FDW-6340). Every call is a list of values (a single value is a list of one, a low and a high are
/// two), so a kind states one minimum and maximum and the count is checked once, here. A kind adds one condition to the filter.
/// </summary>
public abstract class RuleComparisonBase : TypeOptionBase<RuleComparisonBase>, IRuleComparison
{
    private const string ToMethod = "To";

    /// <summary>Initializes a new instance of the <see cref="RuleComparisonBase"/> class.</summary>
    /// <param name="name">The kind's name.</param>
    /// <param name="minValues">The fewest values it takes.</param>
    /// <param name="maxValues">The most values it takes, or <see langword="null"/> for a list of any length.</param>
    /// <param name="displayName">The name a console shows for the kind.</param>
    /// <param name="description">What the kind does, in a sentence a person choosing it reads as a hint.</param>
    protected RuleComparisonBase(string name, int minValues, int? maxValues, string displayName, string description)
        : base(name, displayName, description, null)
    {
        MinValues = minValues;
        MaxValues = maxValues;
    }

    /// <inheritdoc />
    public string Method => ToMethod;

    /// <inheritdoc />
    public int MinValues { get; }

    /// <inheritdoc />
    public int? MaxValues { get; }

    /// <inheritdoc />
    public IReadOnlyList<IRuleLayer> Layers => [];

    /// <inheritdoc />
    public IGenericResult<FilterExpression> To<T>(ILogger logger, FilterExpression target, string field, T value)
        where T : IEquatable<T>, IComparable<T> =>
        Apply(logger, target, field, [value]);

    /// <inheritdoc />
    public IGenericResult<FilterExpression> To<T>(ILogger logger, FilterExpression target, string field, IReadOnlyList<T> values)
        where T : IEquatable<T>, IComparable<T> =>
        Apply(logger, target, field, [.. values.Select(value => (object?)value)]);

    /// <inheritdoc />
    public IGenericResult<FilterExpression> To<T>(ILogger logger, FilterExpression target, string field, T low, T high)
        where T : IEquatable<T>, IComparable<T> =>
        Apply(logger, target, field, [low, high]);

    /// <summary>Adds the kind's condition to <paramref name="target"/>; the value count is already checked.</summary>
    /// <param name="target">The filter so far.</param>
    /// <param name="field">The column the rule is about.</param>
    /// <param name="values">The values, between <see cref="MinValues"/> and <see cref="MaxValues"/> of them.</param>
    /// <returns>The filter with the condition added.</returns>
    public abstract FilterExpression Append(FilterExpression target, string field, IReadOnlyList<object?> values);

    /// <summary>Gives <paramref name="target"/> with one more condition, joined with AND.</summary>
    /// <param name="target">The filter so far.</param>
    /// <param name="field">The column.</param>
    /// <param name="comparison">The operator of the condition.</param>
    /// <param name="value">The value of the condition: one value, or the list for In.</param>
    /// <returns>The filter with the condition added.</returns>
    protected static FilterExpression And(FilterExpression target, string field, IFilterOperator comparison, object? value)
    {
        var condition = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = field, Operator = comparison, Value = value },
        };
        return target.Root is null ? condition : FilterExpressionExtensions.And(target, condition);
    }

    private IGenericResult<FilterExpression> Apply(ILogger logger, FilterExpression target, string field, IReadOnlyList<object?> values)
    {
        if (values.Count < MinValues || (MaxValues is { } maximum && values.Count > maximum))
        {
            var limit = MaxValues?.ToString(CultureInfo.InvariantCulture) ?? DataOptionPlaceholders.AnyNumber;
            DataOptionLog.ValueCountOutOfRange(logger, Name, field, MinValues, limit, values.Count);
            return GenericResult<FilterExpression>.Failure(
                DataOptionResultCodes.ByName("DataOptionValueCountOutOfRange"),
                ResultDetails.Create("Kind", Name, "Field", field).With("Minimum", MinValues).With("Maximum", limit).With("Count", values.Count));
        }

        DataOptionLog.KindApplied(logger, Name, field, values.Count);
        return GenericResult<FilterExpression>.Success(Append(target, field, values));
    }
}
