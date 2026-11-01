using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Turns the table inputs, derived values and table outputs a request states into a calculation set definition. A member that
/// belongs to another kind of row source or column is refused, never ignored.
/// </summary>
internal static class BuiltSetMapper
{
    internal static IGenericResult<CalculationSetDefinition> ToDefinition(
        string name,
        IList<TableInputPayload> tables,
        IList<DerivedValuePayload> derived,
        IList<TableOutputPayload> outputs,
        int maxDepth,
        ILogger logger)
    {
        var tableDefinitions = tables
            .Select(table => new TableInputDefinition(table.Name, [.. table.Columns.Select(column => new TableColumnDefinition(column.Name, column.ValueType))]))
            .ToList();
        var values = new List<DerivedValueDefinition>(derived.Count);
        foreach (var value in derived)
        {
            var mapped = Derived(value, logger);
            if (mapped.IsFailure)
            {
                return mapped.ToNewResult<CalculationSetDefinition>();
            }

            values.Add(mapped.Value!);
        }

        var tableOutputs = new List<TableOutputDefinition>(outputs.Count);
        foreach (var output in outputs)
        {
            var mapped = Output(output, maxDepth, logger);
            if (mapped.IsFailure)
            {
                return mapped.ToNewResult<CalculationSetDefinition>();
            }

            tableOutputs.Add(mapped.Value!);
        }

        return GenericResult<CalculationSetDefinition>.Success(new CalculationSetDefinition(name, tableDefinitions, values, tableOutputs));
    }

    private static IGenericResult<DerivedValueDefinition> Derived(DerivedValuePayload value, ILogger logger)
    {
        IGenericResult<DerivedValueDefinition> Refused(string reason) =>
            GenericResult<DerivedValueDefinition>.Failure(BuiltTransformationEndpointLog.DerivedInvalid(logger, value.Name, reason));

        DerivedValueDefinition? made = value.Kind switch
        {
            "Sum" => new SumValue(value.Name, value.Table, value.Column),
            "Count" => new CountValue(value.Name, value.Table, value.Column),
            "Min" => new MinValue(value.Name, value.Table, value.Column),
            "Max" => new MaxValue(value.Name, value.Table, value.Column),
            "Average" => new AverageValue(value.Name, value.Table, value.Column),
            "Lookup" when value.KeyColumn is { } key && value.MatchField is { } match => new LookupValue(value.Name, value.Table, key, match, value.Column),
            _ => null,
        };
        if (made is null)
        {
            return Refused(string.Equals(value.Kind, "Lookup", StringComparison.Ordinal) ? "a Lookup states its keyColumn and matchField" : $"'{value.Kind}' is not Sum, Count, Min, Max, Average or Lookup");
        }

        var lookupOnly = !string.Equals(value.Kind, "Lookup", StringComparison.Ordinal) && (value.KeyColumn is not null || value.MatchField is not null);
        return lookupOnly ? Refused("keyColumn and matchField belong to a Lookup") : GenericResult<DerivedValueDefinition>.Success(made);
    }

    private static IGenericResult<TableOutputDefinition> Output(TableOutputPayload output, int maxDepth, ILogger logger)
    {
        IGenericResult<TableOutputDefinition> Refused(string reason) =>
            GenericResult<TableOutputDefinition>.Failure(BuiltTransformationEndpointLog.OutputInvalid(logger, output.Name, reason));

        if (output.Rows is null)
        {
            return Refused("it states no rows source");
        }

        var rows = Rows(output.Name, output.Rows, maxDepth, logger);
        if (rows.IsFailure)
        {
            return rows.ToNewResult<TableOutputDefinition>();
        }

        var columns = new List<OutputColumnDefinition>(output.Columns.Count);
        foreach (var column in output.Columns)
        {
            var mapped = Column(output.Name, column, maxDepth, logger);
            if (mapped.IsFailure)
            {
                return mapped.ToNewResult<TableOutputDefinition>();
            }

            columns.Add(mapped.Value!);
        }

        return GenericResult<TableOutputDefinition>.Success(new TableOutputDefinition(output.Name, rows.Value!, columns));
    }

    private static IGenericResult<RowSourceDefinition> Rows(string output, RowSourcePayload rows, int maxDepth, ILogger logger) =>
        rows.Kind switch
        {
            "Each" when rows.Table is { } table => EachRows(output, rows, table, maxDepth, logger),
            "Count" when rows.Count is { } count && !string.IsNullOrWhiteSpace(rows.IndexColumn) => CountRows(output, rows, count, maxDepth, logger),
            "EachGroup" when rows.Table is { } grouped && rows.By is { Count: > 0 } by => GroupRows(output, rows, grouped, by, logger),
            _ => RowsRefused(output, $"the rows source '{rows.Kind}' is not Each (with a table), Count (with a count formula and an indexColumn) or EachGroup (with a table and by columns)", logger),
        };

    private static IGenericResult<RowSourceDefinition> EachRows(string output, RowSourcePayload rows, string table, int maxDepth, ILogger logger)
    {
        if (rows.Count is not null || rows.IndexColumn is not null || rows.By is { Count: > 0 })
        {
            return RowsRefused(output, "an Each source takes a table and optionally a where condition, not count, indexColumn or by", logger);
        }

        if (rows.Where is null)
        {
            return GenericResult<RowSourceDefinition>.Success(new EachRowSource(table, null));
        }

        var where = BuiltFormulaMapper.ToNode(string.Concat(output, "/where"), rows.Where, maxDepth, logger);
        return where.IsFailure ? where.ToNewResult<RowSourceDefinition>() : GenericResult<RowSourceDefinition>.Success(new EachRowSource(table, where.Value!));
    }

    private static IGenericResult<RowSourceDefinition> CountRows(string output, RowSourcePayload rows, BuiltFormulaNodePayload count, int maxDepth, ILogger logger)
    {
        if (rows.Where is not null || rows.By is { Count: > 0 })
        {
            return RowsRefused(output, "a Count source takes a count formula and an indexColumn, and optionally a table, not where or by", logger);
        }

        var node = BuiltFormulaMapper.ToNode(string.Concat(output, "/count"), count, maxDepth, logger);
        return node.IsFailure ? node.ToNewResult<RowSourceDefinition>() : GenericResult<RowSourceDefinition>.Success(new CountSource(node.Value!, rows.IndexColumn!, rows.Table));
    }

    private static IGenericResult<RowSourceDefinition> GroupRows(string output, RowSourcePayload rows, string table, IList<string> by, ILogger logger) =>
        rows.Where is not null || rows.Count is not null || rows.IndexColumn is not null
            ? RowsRefused(output, "an EachGroup source takes a table and the by columns, not where, count or indexColumn", logger)
            : GenericResult<RowSourceDefinition>.Success(new EachGroupSource(table, [.. by]));

    private static IGenericResult<RowSourceDefinition> RowsRefused(string output, string reason, ILogger logger) =>
        GenericResult<RowSourceDefinition>.Failure(BuiltTransformationEndpointLog.OutputInvalid(logger, output, reason));

    private static IGenericResult<OutputColumnDefinition> Column(string output, OutputColumnPayload column, int maxDepth, ILogger logger)
    {
        var problem = ColumnProblem(column);
        if (problem is not null)
        {
            return ColumnRefused(output, column, problem, logger);
        }

        return column.Kind switch
        {
            "Copy" => CopyOf(output, column, logger),
            "Literal" => LiteralOf(output, column, logger),
            _ => FormulaOf(output, column, maxDepth, logger),
        };
    }

    // The members a column's kind does not take, or an unknown kind.
    private static string? ColumnProblem(OutputColumnPayload column) =>
        column.Kind switch
        {
            "Copy" => OnlyTakes("Copy", "a source", column.LiteralType, column.LiteralValue, column.Formula),
            "Literal" => OnlyTakes("Literal", "literalType and literalValue", column.Source, column.Formula),
            "Formula" => OnlyTakes("Formula", "a formula", column.Source, column.LiteralType, column.LiteralValue),
            _ => $"'{column.Kind}' is not Copy, Literal or Formula",
        };

    private static string? OnlyTakes(string kind, string takes, params object?[] others) =>
        others.Any(other => other is not null) ? $"a {kind} column takes only {takes}" : null;

    private static IGenericResult<OutputColumnDefinition> CopyOf(string output, OutputColumnPayload column, ILogger logger) =>
        string.IsNullOrWhiteSpace(column.Source)
            ? ColumnRefused(output, column, "a Copy column states its source", logger)
            : GenericResult<OutputColumnDefinition>.Success(new CopyColumn(column.Name, column.Source));

    private static IGenericResult<OutputColumnDefinition> FormulaOf(string output, OutputColumnPayload column, int maxDepth, ILogger logger)
    {
        if (column.Formula is null)
        {
            return ColumnRefused(output, column, "a Formula column states its formula", logger);
        }

        var node = BuiltFormulaMapper.ToNode(string.Concat(output, "/", column.Name), column.Formula, maxDepth, logger);
        return node.IsFailure ? node.ToNewResult<OutputColumnDefinition>() : GenericResult<OutputColumnDefinition>.Success(new FormulaColumn(column.Name, node.Value!));
    }

    private static IGenericResult<OutputColumnDefinition> LiteralOf(string output, OutputColumnPayload column, ILogger logger)
    {
        if (column.LiteralValue is null)
        {
            return ColumnRefused(output, column, "a Literal column states its literalValue", logger);
        }

        return column.LiteralType switch
        {
            "Text" => GenericResult<OutputColumnDefinition>.Success(new LiteralColumn(column.Name, column.LiteralValue)),
            "Number" when decimal.TryParse(column.LiteralValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) => GenericResult<OutputColumnDefinition>.Success(new LiteralColumn(column.Name, number)),
            "Flag" when bool.TryParse(column.LiteralValue, out var flag) => GenericResult<OutputColumnDefinition>.Success(new LiteralColumn(column.Name, flag)),
            _ => ColumnRefused(output, column, $"'{column.LiteralValue}' is not a {column.LiteralType}; a literal is Text, Number or Flag", logger),
        };
    }

    private static IGenericResult<OutputColumnDefinition> ColumnRefused(string output, OutputColumnPayload column, string reason, ILogger logger) =>
        GenericResult<OutputColumnDefinition>.Failure(BuiltTransformationEndpointLog.OutputInvalid(logger, output, $"column '{column.Name}': {reason}"));
}
