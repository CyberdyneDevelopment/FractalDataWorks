using System;
using System.Collections.Generic;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>
/// The comparisons a threshold field accepts (FDW-4001), decided here once from the type of the value the field reports. The names are those of FilterOperators options. The criteria
/// builder offers a field these and no others, and a stored rule that names another is refused when the threshold is read. IsNull and IsNotNull are not offered (a reported value is
/// always present), nor In, Between and DateRange (a stored condition holds one value).
/// </summary>
public static class ThresholdFieldOperators
{
    private static readonly IReadOnlyList<string> Ordered = ["Equal", "NotEqual", "GreaterThan", "GreaterThanOrEqual", "LessThan", "LessThanOrEqual"];

    private static readonly IReadOnlyList<string> Text = ["Equal", "NotEqual", "Contains", "StartsWith", "EndsWith"];

    private static readonly IReadOnlyList<string> Identity = ["Equal", "NotEqual"];

    /// <summary>Gets every name this class can answer, each once.</summary>
    public static IReadOnlyList<string> All { get; } = ["Equal", "NotEqual", "GreaterThan", "GreaterThanOrEqual", "LessThan", "LessThanOrEqual", "Contains", "StartsWith", "EndsWith"];

    /// <summary>Gets the comparisons a field of a value type accepts.</summary>
    /// <param name="valueType">The type of the value the field reports; a nullable type is read as its underlying type.</param>
    /// <returns>The names of the FilterOperators options, in the order a builder offers them.</returns>
    public static IReadOnlyList<string> For(Type valueType)
    {
        var underlying = Nullable.GetUnderlyingType(valueType);
        var type = underlying is null ? valueType : underlying;
        if (type == typeof(string))
        {
            return Text;
        }

        return IsOrdered(type) ? Ordered : Identity;
    }

    private static bool IsOrdered(Type type) =>
        type == typeof(long) || type == typeof(int) || type == typeof(short) || type == typeof(double) || type == typeof(float) || type == typeof(decimal)
        || type == typeof(TimeSpan) || type == typeof(DateTimeOffset) || type == typeof(DateTime);
}
