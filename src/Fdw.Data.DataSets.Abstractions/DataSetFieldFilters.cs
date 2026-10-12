using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Schema;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// The filter operators a data set field takes: the one place that answers it (FDW-1102). The field read returns the answer
/// so a console shows "filter by" only where it applies, and the query translator refuses an operator outside it.
/// </summary>
/// <remarks>
/// A field takes what its datum type allows, narrowed to what its data type can carry. It takes nothing when it is not
/// filterable, when its datum type is <c>NotYetTyped</c> (which allows nothing until someone types the column), or when its
/// declared data type is not one of the known names. There is no default set: an empty answer is the honest one.
/// <para>
/// <c>Between</c> is the range form of a number (<c>{"start": .., "stop": ..}</c>) and of a date (<c>startDate</c> and
/// <c>endDate</c> on the condition); the data type decides which. <c>NotIn</c> takes a string or whole-number field, the
/// pattern operators a text field, and the four range comparisons any field that is not a boolean, a Guid or bytes.
/// </para>
/// </remarks>
public static class DataSetFieldFilters
{
    private static readonly HashSet<Type> Numbers = [typeof(byte), typeof(short), typeof(int), typeof(long), typeof(decimal), typeof(double), typeof(float)];
    private static readonly HashSet<Type> Dates = [typeof(DateTime), typeof(DateTimeOffset)];
    private static readonly HashSet<Type> WholeNumbersAndText = [typeof(string), typeof(byte), typeof(short), typeof(int), typeof(long)];
    private static readonly HashSet<Type> Text = [typeof(string), typeof(char)];
    private static readonly HashSet<Type> Unordered = [typeof(bool), typeof(Guid), typeof(byte[])];

    // The operators a data type narrows; any operator not named here is taken by every data type.
    private static readonly Dictionary<string, Predicate<Type>> Narrowed = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Between"] = type => Numbers.Contains(type) || Dates.Contains(type),
        ["NotIn"] = WholeNumbersAndText.Contains,
        ["GreaterThan"] = Ordered,
        ["GreaterThanOrEqual"] = Ordered,
        ["LessThan"] = Ordered,
        ["LessThanOrEqual"] = Ordered,
        ["Contains"] = Text.Contains,
        ["StartsWith"] = Text.Contains,
        ["EndsWith"] = Text.Contains,
    };

    /// <summary>Gets the names of the filter operators a field takes.</summary>
    /// <param name="field">The data set field.</param>
    /// <returns>The operator names, in the datum type's order; empty when the field takes none.</returns>
    public static IReadOnlyList<string> Operators(DataSetFieldConfiguration field)
        => Operators(field.IsFilterable, field.DatumTypeId, field.TypeName);

    /// <summary>Gets the names of the filter operators a field takes.</summary>
    /// <param name="isFilterable">Whether the data set's administrator has made the field filterable.</param>
    /// <param name="datumTypeId">The field's datum type id.</param>
    /// <param name="typeName">The field's declared data type name.</param>
    /// <returns>The operator names, in the datum type's order; empty when the field takes none.</returns>
    public static IReadOnlyList<string> Operators(bool isFilterable, int datumTypeId, string? typeName)
    {
        if (!isFilterable) return [];

        var type = DataSetFieldTypes.Resolve(typeName);
        if (type is null) return [];

        return DatumTypes.ById(datumTypeId).AllowedOperators.Where(name => TypeTakes(type, name)).ToList();
    }

    private static bool TypeTakes(Type type, string operatorName)
        => !Narrowed.TryGetValue(operatorName, out var takes) || takes(type);

    private static bool Ordered(Type type) => !Unordered.Contains(type);
}
