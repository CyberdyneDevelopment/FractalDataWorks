using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Collections;

namespace Fdw.Schema;

/// <summary>
/// Base class for datum types. Each option states its behavior in its constructor, so the type
/// collection generator can read it without instantiation, and overrides the two behaviors that are
/// decision rather than a flag (<see cref="IsReturned"/>) where they differ.
/// </summary>
public abstract class DatumTypeBase : TypeOptionBase<DatumTypeBase>, IDatumType
{
    /// <summary>Equality, list membership and presence, for values that identify or group.</summary>
    protected static readonly string[] EqualityOperators = ["Equal", "NotEqual", "In", "NotIn", "IsNull", "IsNotNull"];

    /// <summary>
    /// Equality plus the four range comparisons and Between, for values that order. Between is the range form of a number
    /// (<c>{"start": .., "stop": ..}</c>) and of a date (<c>startDate</c> and <c>endDate</c>); the column's data type picks which.
    /// </summary>
    protected static readonly string[] RangeOperators =
        ["Equal", "NotEqual", "In", "NotIn", "GreaterThan", "GreaterThanOrEqual", "LessThan", "LessThanOrEqual", "Between", "IsNull", "IsNotNull"];

    /// <summary>Equality plus text matching, for values read as text.</summary>
    protected static readonly string[] TextOperators =
        ["Equal", "NotEqual", "In", "NotIn", "Contains", "StartsWith", "EndsWith", "IsNull", "IsNotNull"];

    /// <summary>No operator: a column whose data nobody has described yet takes no filter until it is typed.</summary>
    protected static readonly string[] NoOperators = [];

    /// <summary>Initializes a new instance of the <see cref="DatumTypeBase"/> class.</summary>
    /// <param name="name">Name of the datum type (must match its TypeOption attribute).</param>
    /// <param name="description">What data of this type is.</param>
    /// <param name="isKeyRole">Whether a column of this type identifies its record.</param>
    /// <param name="isIndexable">Whether a column of this type is worth an index.</param>
    /// <param name="isAggregatable">Whether a column of this type can be aggregated.</param>
    /// <param name="isOrderable">Whether a result can be ordered by a column of this type.</param>
    /// <param name="isGroupable">Whether a result can be grouped by a column of this type.</param>
    /// <param name="isPointInTime">Whether a column of this type is a point in time that describes the record.</param>
    /// <param name="isLowerDateBoundary">Whether a column of this type is where a record's validity period starts.</param>
    /// <param name="isUpperDateBoundary">Whether a column of this type is where a record's validity period ends.</param>
    /// <param name="isDisplayable">Whether a column of this type may be shown to a person.</param>
    /// <param name="allowedOperators">
    /// The FilterOperators a filter on a column of this type may use; empty when the type allows none. An array, not a list: the generated
    /// NotFound option calls this constructor with defaults, and only an array has one (empty) that is not TC009.
    /// </param>
    protected DatumTypeBase(
        string name,
        string description,
        bool isKeyRole,
        bool isIndexable,
        bool isAggregatable,
        bool isOrderable,
        bool isGroupable,
        bool isPointInTime,
        bool isLowerDateBoundary,
        bool isUpperDateBoundary,
        bool isDisplayable,
        string[] allowedOperators)
        : base(name, $"DatumTypes:{name}", name, description, "DatumType")
    {
        IsKeyRole = isKeyRole;
        IsIndexable = isIndexable;
        IsAggregatable = isAggregatable;
        IsOrderable = isOrderable;
        IsGroupable = isGroupable;
        IsPointInTime = isPointInTime;
        IsLowerDateBoundary = isLowerDateBoundary;
        IsUpperDateBoundary = isUpperDateBoundary;
        IsDisplayable = isDisplayable;
        AllowedOperators = allowedOperators;
    }

    /// <inheritdoc />
    public bool IsKeyRole { get; }

    /// <inheritdoc />
    public bool IsIndexable { get; }

    /// <inheritdoc />
    public bool IsAggregatable { get; }

    /// <inheritdoc />
    public bool IsOrderable { get; }

    /// <inheritdoc />
    public bool IsGroupable { get; }

    /// <inheritdoc />
    public bool IsPointInTime { get; }

    /// <inheritdoc />
    public bool IsLowerDateBoundary { get; }

    /// <inheritdoc />
    public bool IsUpperDateBoundary { get; }

    /// <inheritdoc />
    public bool IsDisplayable { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> AllowedOperators { get; }

    /// <inheritdoc />
    /// <remarks>
    /// Read off the declared flags: a point in time or either end of a period sits on Time, a number that can be
    /// aggregated on Measure, and any other groupable type on Dimension. A type that says something else (Place)
    /// overrides this.
    /// </remarks>
    public virtual IReadOnlyList<string> Axes
    {
        get
        {
            if (IsPointInTime || IsLowerDateBoundary || IsUpperDateBoundary) return [AxisTypes.Time.Name];
            if (IsAggregatable) return [AxisTypes.Measure.Name];
            return IsGroupable ? [AxisTypes.Dimension.Name] : [];
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Count of any type; Sum and Avg of an aggregatable one; Min and Max of an orderable one. These are the rules
    /// the data route enforced for a grouped read before they were declared here.
    /// </remarks>
    public virtual IReadOnlyList<string> Aggregates
    {
        get
        {
            var allowed = new List<string> { DatumAggregates.Count };
            if (IsAggregatable) allowed.AddRange([DatumAggregates.Sum, DatumAggregates.Avg]);
            if (IsOrderable) allowed.AddRange([DatumAggregates.Min, DatumAggregates.Max]);
            return allowed;
        }
    }

    /// <inheritdoc />
    public bool AcceptsAxis(string axis) => Axes.Contains(axis, StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public bool AllowsAggregate(string aggregate) => Aggregates.Contains(aggregate, StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    /// <remarks>What <see cref="IsReturned"/> answers when the command includes nothing extra.</remarks>
    public bool IsDefaultResult => IsReturned(includeMetadata: false, includeDescriptions: false, includeKeys: false);

    /// <inheritdoc />
    /// <remarks>A detail column is returned whether or not anything extra was asked for.</remarks>
    public virtual bool IsReturned(bool includeMetadata, bool includeDescriptions, bool includeKeys) => true;
}
