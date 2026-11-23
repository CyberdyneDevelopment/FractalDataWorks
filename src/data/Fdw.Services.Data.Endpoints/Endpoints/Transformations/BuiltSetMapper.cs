using System.Collections.Generic;
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
        var mapper = DerivedValueMappers.ByName(value.Kind);
        return ReferenceEquals(mapper, DerivedValueMappers.NotFound)
            ? GenericResult<DerivedValueDefinition>.Failure(
                BuiltTransformationEndpointLog.DerivedInvalid(logger, value.Name, $"'{value.Kind}' is not {KindNames.Of(DerivedValueMappers.All().Select(kind => kind.Name))}"))
            : mapper.Map(value, logger);
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

    private static IGenericResult<RowSourceDefinition> Rows(string output, RowSourcePayload rows, int maxDepth, ILogger logger)
    {
        var mapper = RowSourceMappers.ByName(rows.Kind);
        return ReferenceEquals(mapper, RowSourceMappers.NotFound)
            ? RowSourceMapperBase.NotStated(output, rows.Kind, logger)
            : mapper.Map(output, rows, maxDepth, logger);
    }

    private static IGenericResult<OutputColumnDefinition> Column(string output, OutputColumnPayload column, int maxDepth, ILogger logger)
    {
        var mapper = OutputColumnMappers.ByName(column.Kind);
        return ReferenceEquals(mapper, OutputColumnMappers.NotFound)
            ? OutputColumnMapperBase.Refused(
                output, column, $"'{column.Kind}' is not {KindNames.Of(OutputColumnMappers.All().Select(kind => kind.Name))}", logger)
            : mapper.Map(output, column, maxDepth, logger);
    }
}
