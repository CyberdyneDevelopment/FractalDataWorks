using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Runtime;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Logging;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Runs a Compute stage whose Built transformation reads and writes tables (FDW-1231): fills each table from the place the stage
/// names, hands the single values over, runs the compiled set, and sends each output where the stage routes it. The
/// transformation itself never learns where rows came from or went.
/// </summary>
internal static class ComputeSetRunner
{
    private const string StageSource = "Stage";
    private const string DataSetSource = "DataSet";
    private const string ReplaceRoute = "Replace";

    internal static async Task<IGenericResult<IEnumerable<IDictionary<string, object?>>>> Run(
        PipelineTransformConfiguration config,
        ICompiledCalculationSet set,
        IEnumerable<IDictionary<string, object?>> inputs,
        ITransformContext context,
        CancellationToken cancellationToken)
    {
        var incoming = inputs.ToList();
        var scalars = Scalars(config, context);
        if (scalars.IsFailure)
        {
            return scalars.ToNewResult<IEnumerable<IDictionary<string, object?>>>();
        }

        var tables = await Tables(config, incoming, context, cancellationToken).ConfigureAwait(false);
        if (tables.IsFailure)
        {
            return tables.ToNewResult<IEnumerable<IDictionary<string, object?>>>();
        }

        var written = set.Evaluate(scalars.Value!, tables.Value!, RowHandler(config, context));
        if (written.IsFailure)
        {
            return written.ToNewResult<IEnumerable<IDictionary<string, object?>>>();
        }

        var routed = Route(config, incoming, written.Value!.Tables, context);
        var rowsOut = written.Value.Tables.Values.Sum(rows => rows.Count);
        ComputeSetLog.SetApplied(context.Logger, config.Name, incoming.Count, written.Value.Tables.Count, rowsOut);
        return routed;
    }

    // The stage's rule for a row the set cannot build. Fail stops the stage; Skip reports the row as an error of the run and
    // drops it; Reject rejects it with its code. The run start already refused any other value.
    private static SetRowFailureHandler RowHandler(PipelineTransformConfiguration config, ITransformContext context) =>
        failure =>
        {
            if (string.Equals(config.ComputeOnRowFailure, RowFailureActions.Fail, StringComparison.Ordinal))
            {
                return false;
            }

            var record = new Dictionary<string, object?>(failure.Row, StringComparer.OrdinalIgnoreCase);
            ComputeSetLog.RowDropped(context.Logger, config.Name, failure.Output, config.ComputeOnRowFailure!, failure.Reason);
            if (string.Equals(config.ComputeOnRowFailure, RowFailureActions.Reject, StringComparison.Ordinal))
            {
                context.Reject(config.Name, failure.Reason, record);
            }
            else
            {
                context.ReportError(failure.Reason, record);
            }

            return true;
        };

    private static IGenericResult<IReadOnlyDictionary<string, object?>> Scalars(PipelineTransformConfiguration config, ITransformContext context)
    {
        var values = new Dictionary<string, object?>(config.ComputeScalars.Count, StringComparer.Ordinal);
        foreach (var scalar in config.ComputeScalars)
        {
            if (!ComputeScalarValue.TryParse(scalar.ValueText, scalar.ValueType, out var value))
            {
                return GenericResult<IReadOnlyDictionary<string, object?>>.Failure(
                    ComputeSetLog.ScalarInvalid(context.Logger, config.Name, scalar.InputName, scalar.ValueText, scalar.ValueType));
            }

            values[scalar.InputName] = value;
        }

        return GenericResult<IReadOnlyDictionary<string, object?>>.Success(values);
    }

    private static async Task<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>>>> Tables(
        PipelineTransformConfiguration config,
        List<IDictionary<string, object?>> incoming,
        ITransformContext context,
        CancellationToken cancellationToken)
    {
        var tables = new Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>>(StringComparer.Ordinal);
        foreach (var bound in config.ComputeTables)
        {
            var rows = await RowsOf(config, bound, incoming, context, cancellationToken).ConfigureAwait(false);
            if (rows.IsFailure)
            {
                return rows.ToNewResult<IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>>>();
            }

            tables[bound.TableName] = rows.Value!;
        }

        return GenericResult<IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>>>.Success(tables);
    }

    private static async Task<IGenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>> RowsOf(
        PipelineTransformConfiguration config,
        PipelineTransformComputeTableConfiguration bound,
        List<IDictionary<string, object?>> incoming,
        ITransformContext context,
        CancellationToken cancellationToken)
    {
        if (string.Equals(bound.FromKind, StageSource, StringComparison.Ordinal))
        {
            return GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Success(
                [.. incoming.Select(row => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>(row, StringComparer.Ordinal))]);
        }

        if (string.Equals(bound.FromKind, DataSetSource, StringComparison.Ordinal))
        {
            return await ComputeDataSetReader.ReadAll(context.DataGateway, config.Name, bound.TableName, bound.FromName!, context.Logger, cancellationToken).ConfigureAwait(false);
        }

        return context.SharedState.TryGetValue(OutputKey(bound.FromName!), out var kept) && kept is List<IReadOnlyDictionary<string, object?>> rows
            ? GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Success(rows)
            : GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Failure(ComputeSetLog.NamedOutputMissing(context.Logger, config.Name, bound.FromName!, bound.TableName));
    }

    // The output routed to Replace becomes the stage's rows; the others are kept under their names, added to what an earlier
    // output of that name already holds. With no Replace route the rows that arrived carry on unchanged.
    private static IGenericResult<IEnumerable<IDictionary<string, object?>>> Route(
        PipelineTransformConfiguration config,
        List<IDictionary<string, object?>> incoming,
        IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>> written,
        ITransformContext context)
    {
        IEnumerable<IDictionary<string, object?>> stageRows = incoming;
        foreach (var route in config.ComputeOutputs)
        {
            var rows = written[route.OutputName];
            if (string.Equals(route.Route, ReplaceRoute, StringComparison.Ordinal))
            {
                stageRows = [.. rows.Select(row => (IDictionary<string, object?>)new Dictionary<string, object?>(row, StringComparer.OrdinalIgnoreCase))];
                continue;
            }

            var key = OutputKey(route.RouteName!);
            if (!context.SharedState.TryGetValue(key, out var kept) || kept is not List<IReadOnlyDictionary<string, object?>> keptRows)
            {
                keptRows = [];
                context.SharedState[key] = keptRows;
            }

            keptRows.AddRange(rows);
        }

        return GenericResult<IEnumerable<IDictionary<string, object?>>>.Success(stageRows);
    }

    private static string OutputKey(string name) => string.Concat("compute.output.", name);
}
