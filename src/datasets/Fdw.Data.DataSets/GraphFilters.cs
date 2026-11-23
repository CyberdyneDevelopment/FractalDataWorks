using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// A converted data set's stored filters as one Graph Filter component (FDW-6972, FDW-6973): the conditions joined by <c>and</c> in the pipeline's
/// condition language (the Filter stage's <c>filterExpression</c>, read by RecordPredicates), placed after the last component so every field is
/// filtered by the name it carries there.
/// </summary>
/// <remarks>
/// Only what that language can state is written: Equal <c>==</c>, NotEqual <c>!=</c>, GreaterThan <c>&gt;</c>, GreaterThanOrEqual <c>&gt;=</c>,
/// LessThan <c>&lt;</c>, LessThanOrEqual <c>&lt;=</c>, Contains, StartsWith, EndsWith, IsNull <c>== null</c> and IsNotNull <c>!= null</c>. Any
/// other operator, a field the stream does not carry or whose name is not a plain name, and a text value holding both quote characters (the
/// language has no escape) are refused by name, never dropped. A number or a true/false value is written bare, so the comparison stays typed;
/// any other value is quoted.
/// </remarks>
public static class GraphFilters
{
    /// <summary>The name of the Filter component a conversion adds.</summary>
    public const string ComponentName = "filter";

    private static readonly Dictionary<string, string> Comparisons = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Equal"] = "==",
        ["NotEqual"] = "!=",
        ["GreaterThan"] = ">",
        ["GreaterThanOrEqual"] = ">=",
        ["LessThan"] = "<",
        ["LessThanOrEqual"] = "<=",
        ["Contains"] = "contains",
        ["StartsWith"] = "startswith",
        ["EndsWith"] = "endswith",
    };

    private static readonly Dictionary<string, string> NullChecks = new(StringComparer.OrdinalIgnoreCase)
    {
        ["IsNull"] = "== null",
        ["IsNotNull"] = "!= null",
    };

    // The data types whose values are written bare, so the condition compares them as what they are and not as text.
    private static readonly HashSet<string> BareTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Byte", "Int16", "Int32", "Int64", "Single", "Double", "Decimal", "Boolean",
    };

    /// <summary>Builds the Filter component for the stored filters, or names each condition the condition language cannot state.</summary>
    /// <param name="filters">The data set's stored filter conditions.</param>
    /// <param name="stream">The name each logical field carries in the stream the filter reads.</param>
    /// <param name="components">The names of the components the graph already has; the Filter component's name must not be one of them.</param>
    /// <param name="refused">Receives a description of each condition that cannot be written.</param>
    /// <returns>The component, or null when the data set has no current filter or a condition was refused.</returns>
    public static GraphDataSetComponentConfiguration? Component(
        IEnumerable<DataSetFilterConditionConfiguration> filters, IReadOnlyDictionary<string, string> stream, IReadOnlyCollection<string> components, List<string> refused)
    {
        var conditions = filters.Where(f => f.IsCurrent && !f.IsDeleted).OrderBy(f => f.Ordinal).ToList();
        if (conditions.Count == 0)
            return null;

        var written = new List<string>(conditions.Count);
        var before = refused.Count;
        if (components.Contains(ComponentName, StringComparer.OrdinalIgnoreCase))
            refused.Add($"the filters (a component is already named '{ComponentName}', the name the Filter component takes)");
        foreach (var condition in conditions)
        {
            if (Condition(condition, stream) is { } text)
                written.Add(text);
            else
                refused.Add($"filter on '{condition.FieldName}' ({condition.Operator} {condition.Value})");
        }

        if (refused.Count > before)
            return null;

        return new GraphDataSetComponentConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = ComponentName,
            ComponentType = "Filter",
            Stages =
            [
                new GraphDataSetStageConfiguration
                {
                    Id = Guid.CreateVersion7(),
                    Name = ComponentName,
                    OperationType = "Filter",
                    ExecutionOrder = 0,
                    IsEnabled = true,
                    FilterExpression = string.Join(" and ", written),
                },
            ],
        };
    }

    // One condition in the language, or null when the language cannot state it.
    private static string? Condition(DataSetFilterConditionConfiguration condition, IReadOnlyDictionary<string, string> stream)
    {
        if (!stream.TryGetValue(condition.FieldName, out var field) || !IsPlainName(field))
            return null;

        if (NullChecks.TryGetValue(condition.Operator, out var check))
            return $"{field} {check}";

        if (!Comparisons.TryGetValue(condition.Operator, out var comparison) || condition.Value is not { } value)
            return null;

        return Value(value, condition.DataType) is { } written ? $"{field} {comparison} {written}" : null;
    }

    // A number or a true/false value bare; any other value in the quote it does not hold; null when it holds both.
    private static string? Value(string value, string dataType)
    {
        if (BareTypes.Contains(dataType) && value.Length > 0 && !value.Any(char.IsWhiteSpace))
            return value;
        if (!value.Contains('"', StringComparison.Ordinal))
            return $"\"{value}\"";
        return value.Contains('\'', StringComparison.Ordinal) ? null : $"'{value}'";
    }

    private static bool IsPlainName(string name) =>
        name.Length > 0 && (char.IsLetter(name[0]) || name[0] == '_') && name.All(c => char.IsLetterOrDigit(c) || c == '_');
}
