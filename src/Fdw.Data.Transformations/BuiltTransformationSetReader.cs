using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Data.Transformations;

/// <summary>
/// Reads the stored table inputs, derived values and table outputs of a Built transformation into a calculation set
/// definition. It refuses, with a coded result, rows that cannot make a definition; it does not repair them.
/// </summary>
public static class BuiltTransformationSetReader
{
    private const string ColumnRole = "Column";
    private const string WhereRole = "Where";
    private const string CountRole = "Count";

    private static readonly Dictionary<string, Func<BuiltTransformationDerivedConfiguration, DerivedValueDefinition>> Aggregates =
        new(StringComparer.Ordinal)
        {
            ["Sum"] = row => new SumValue(row.Name, row.TableName, row.ColumnName),
            ["Count"] = row => new CountValue(row.Name, row.TableName, row.ColumnName),
            ["Min"] = row => new MinValue(row.Name, row.TableName, row.ColumnName),
            ["Max"] = row => new MaxValue(row.Name, row.TableName, row.ColumnName),
            ["Average"] = row => new AverageValue(row.Name, row.TableName, row.ColumnName),
        };

    /// <summary>Reads the set the stored rows describe.</summary>
    /// <param name="built">The Built transformation as stored.</param>
    /// <param name="limits">The stated limits; the depth limit bounds how far a stored formula is followed.</param>
    /// <returns>The definition, or the coded refusal naming what is wrong with the rows.</returns>
    public static IGenericResult<CalculationSetDefinition> ToDefinition(BuiltTransformationConfiguration built, CalculationLimits limits)
    {
        var derived = ReadDerived(built);
        if (derived.IsFailure)
        {
            return derived.ToNewResult<CalculationSetDefinition>();
        }

        var outputs = ReadOutputs(built, limits);
        return outputs.IsFailure
            ? outputs.ToNewResult<CalculationSetDefinition>()
            : GenericResult<CalculationSetDefinition>.Success(new CalculationSetDefinition(built.Name, ReadTables(built), derived.Value!, outputs.Value!));
    }

    private static List<TableInputDefinition> ReadTables(BuiltTransformationConfiguration built) =>
    [
        .. built.TableInputs.OrderBy(table => table.ExecutionOrder).Select(table => new TableInputDefinition(
            table.Name,
            [.. built.TableInputColumns
                .Where(column => string.Equals(column.TableName, table.Name, StringComparison.Ordinal))
                .OrderBy(column => column.ExecutionOrder)
                .Select(column => new TableColumnDefinition(column.Name, column.ValueType))])),
    ];

    private static IGenericResult<IReadOnlyList<DerivedValueDefinition>> ReadDerived(BuiltTransformationConfiguration built)
    {
        var values = new List<DerivedValueDefinition>(built.DerivedValues.Count);
        foreach (var row in built.DerivedValues.OrderBy(candidate => candidate.ExecutionOrder))
        {
            var value = ReadOneDerived(built.Name, row);
            if (value.IsFailure)
            {
                return value.ToNewResult<IReadOnlyList<DerivedValueDefinition>>();
            }

            values.Add(value.Value!);
        }

        return GenericResult<IReadOnlyList<DerivedValueDefinition>>.Success(values);
    }

    private static IGenericResult<DerivedValueDefinition> ReadOneDerived(string owner, BuiltTransformationDerivedConfiguration row)
    {
        if (Aggregates.TryGetValue(row.Kind, out var make))
        {
            return GenericResult<DerivedValueDefinition>.Success(make(row));
        }

        if (!string.Equals(row.Kind, "Lookup", StringComparison.Ordinal))
        {
            return Invalid<DerivedValueDefinition>(owner, $"derived value '{row.Name}' has the unknown kind '{row.Kind}'");
        }

        return row.KeyColumn is { } key && row.MatchField is { } match
            ? GenericResult<DerivedValueDefinition>.Success(new LookupValue(row.Name, row.TableName, key, match, row.ColumnName))
            : Invalid<DerivedValueDefinition>(owner, $"lookup '{row.Name}' lacks its key column or the value it searches for");
    }

    private static IGenericResult<IReadOnlyList<TableOutputDefinition>> ReadOutputs(BuiltTransformationConfiguration built, CalculationLimits limits)
    {
        var outputs = new List<TableOutputDefinition>(built.TableOutputs.Count);
        foreach (var row in built.TableOutputs.OrderBy(candidate => candidate.ExecutionOrder))
        {
            var output = ReadOneOutput(built, row, limits);
            if (output.IsFailure)
            {
                return output.ToNewResult<IReadOnlyList<TableOutputDefinition>>();
            }

            outputs.Add(output.Value!);
        }

        return GenericResult<IReadOnlyList<TableOutputDefinition>>.Success(outputs);
    }

    private static IGenericResult<TableOutputDefinition> ReadOneOutput(
        BuiltTransformationConfiguration built, BuiltTransformationTableOutputConfiguration row, CalculationLimits limits)
    {
        var source = ReadSource(built, row, limits);
        if (source.IsFailure)
        {
            return source.ToNewResult<TableOutputDefinition>();
        }

        var columns = ReadColumns(built, row, limits);
        return columns.IsFailure
            ? columns.ToNewResult<TableOutputDefinition>()
            : GenericResult<TableOutputDefinition>.Success(new TableOutputDefinition(row.Name, source.Value!, columns.Value!));
    }

    private static IGenericResult<RowSourceDefinition> ReadSource(
        BuiltTransformationConfiguration built, BuiltTransformationTableOutputConfiguration row, CalculationLimits limits)
    {
        if (string.Equals(row.RowSource, "Each", StringComparison.Ordinal) && row.SourceTable is { } each)
        {
            var where = Formula(built, row.Name, null, WhereRole, limits, isRequired: false);
            return where.IsFailure
                ? where.ToNewResult<RowSourceDefinition>()
                : GenericResult<RowSourceDefinition>.Success(new EachRowSource(each, where.Value));
        }

        if (string.Equals(row.RowSource, "Count", StringComparison.Ordinal) && row.IndexColumn is { } index)
        {
            var count = Formula(built, row.Name, null, CountRole, limits, isRequired: true);
            return count.IsFailure
                ? count.ToNewResult<RowSourceDefinition>()
                : GenericResult<RowSourceDefinition>.Success(new CountSource(count.Value!, index, row.SourceTable));
        }

        if (string.Equals(row.RowSource, "EachGroup", StringComparison.Ordinal) && row.SourceTable is { } grouped)
        {
            var by = built.TableOutputGroupColumns
                .Where(column => string.Equals(column.OutputName, row.Name, StringComparison.Ordinal))
                .OrderBy(column => column.Ordinal)
                .Select(column => column.ColumnName)
                .ToList();
            return GenericResult<RowSourceDefinition>.Success(new EachGroupSource(grouped, by));
        }

        return Invalid<RowSourceDefinition>(built.Name, $"output '{row.Name}' has the row source '{row.RowSource}' without what that source needs");
    }

    private static IGenericResult<IReadOnlyList<OutputColumnDefinition>> ReadColumns(
        BuiltTransformationConfiguration built, BuiltTransformationTableOutputConfiguration output, CalculationLimits limits)
    {
        var columns = new List<OutputColumnDefinition>();
        foreach (var row in built.TableOutputColumns
            .Where(column => string.Equals(column.OutputName, output.Name, StringComparison.Ordinal))
            .OrderBy(column => column.ExecutionOrder))
        {
            var column = ReadOneColumn(built, row, limits);
            if (column.IsFailure)
            {
                return column.ToNewResult<IReadOnlyList<OutputColumnDefinition>>();
            }

            columns.Add(column.Value!);
        }

        return GenericResult<IReadOnlyList<OutputColumnDefinition>>.Success(columns);
    }

    private static IGenericResult<OutputColumnDefinition> ReadOneColumn(
        BuiltTransformationConfiguration built, BuiltTransformationTableOutputColumnConfiguration row, CalculationLimits limits)
    {
        if (string.Equals(row.Kind, "Copy", StringComparison.Ordinal) && row.SourceName is { } source)
        {
            return GenericResult<OutputColumnDefinition>.Success(new CopyColumn(row.Name, source));
        }

        if (string.Equals(row.Kind, "Literal", StringComparison.Ordinal))
        {
            return ReadLiteral(built.Name, row);
        }

        if (string.Equals(row.Kind, "Formula", StringComparison.Ordinal))
        {
            var formula = Formula(built, row.OutputName, row.Name, ColumnRole, limits, isRequired: true);
            return formula.IsFailure
                ? formula.ToNewResult<OutputColumnDefinition>()
                : GenericResult<OutputColumnDefinition>.Success(new FormulaColumn(row.Name, formula.Value!));
        }

        return Invalid<OutputColumnDefinition>(built.Name, $"column '{row.Name}' of output '{row.OutputName}' has the kind '{row.Kind}' without what that kind needs");
    }

    private static IGenericResult<OutputColumnDefinition> ReadLiteral(string owner, BuiltTransformationTableOutputColumnConfiguration row)
    {
        if (row.LiteralText is null)
        {
            return GenericResult<OutputColumnDefinition>.Success(new LiteralColumn(row.Name, null));
        }

        return row.LiteralType switch
        {
            "Text" => GenericResult<OutputColumnDefinition>.Success(new LiteralColumn(row.Name, row.LiteralText)),
            "Number" when decimal.TryParse(row.LiteralText, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) =>
                GenericResult<OutputColumnDefinition>.Success(new LiteralColumn(row.Name, number)),
            "Flag" when bool.TryParse(row.LiteralText, out var flag) =>
                GenericResult<OutputColumnDefinition>.Success(new LiteralColumn(row.Name, flag)),
            _ => Invalid<OutputColumnDefinition>(owner, $"column '{row.Name}' of output '{row.OutputName}' states '{row.LiteralText}' as a '{row.LiteralType}', which it is not"),
        };
    }

    // One formula of a table output; an absent optional formula (a row condition) is null, an absent required one is a refusal.
    private static IGenericResult<CalculationNode?> Formula(
        BuiltTransformationConfiguration built, string output, string? column, string role, CalculationLimits limits, bool isRequired)
    {
        var pieces = built.TableOutputNodes
            .Where(row => string.Equals(row.OutputName, output, StringComparison.Ordinal)
                && string.Equals(row.Role, role, StringComparison.Ordinal)
                && string.Equals(row.ColumnName, column, StringComparison.Ordinal))
            .Cast<IBuiltFormulaNodeRow>()
            .ToList();
        if (pieces.Count == 0 && !isRequired)
        {
            return GenericResult<CalculationNode?>.Success(null);
        }

        var tree = BuiltFormulaTrees.Read(built.Name, string.Join('/', new[] { output, column ?? role.ToLowerInvariant() }), pieces, limits.MaxDepth);
        return tree.IsFailure ? tree.ToNewResult<CalculationNode?>() : GenericResult<CalculationNode?>.Success(tree.Value);
    }

    private static IGenericResult<T> Invalid<T>(string owner, string reason) =>
        SetFailure.Of<T>("SetDefinitionInvalid", owner, reason);
}
