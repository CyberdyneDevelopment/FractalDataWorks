using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Calculations;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Etl.Graphs.Describing;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Finds what reads each field of one data set (API-173), for <see cref="FieldLineageResponse.Consumers"/>.
/// </summary>
/// <remarks>
/// <para>
/// Only exact, stored relations are reported, because impact analysis is advice someone acts on:
/// <list type="bullet">
/// <item><description><b>DataSet</b>: another data set with a source whose SourceDataSetName is this one,
/// mapping this field. A DataSet-kind mapping names its upstream field in PhysicalFieldName when set,
/// and otherwise passes the same name through.</description></item>
/// <item><description><b>Calculation</b>: a calculation step operand that reads this field through an
/// input bound to this data set, against the step's output alias.</description></item>
/// </list>
/// </para>
/// <para>
/// A graph pipeline reads a data set whole into its flow, and which of the data set's fields it uses, and what each becomes, is the
/// graph's field flow (what the describe call computes), not a stored relation. It is reported from that flow (FDW-2157): a field is
/// a consumer when it arrives at a destination component with the data set's field as its origin, under the name it arrives as.
/// A pipeline whose fields cannot be followed that way is not guessed at; it is named in <see cref="FieldConsumerScan.PipelinesNotFollowed"/>.
/// </para>
/// <para>
/// Any read that fails fails the scan. An empty consumer list must mean "nothing reads this field",
/// never "the read broke".
/// </para>
/// </remarks>
internal static class FieldConsumerScanner
{
    /// <summary>Consumers of each of <paramref name="fieldNames"/>, keyed by field name (case-insensitive).</summary>
    public static async Task<IGenericResult<FieldConsumerScan>> Scan(
        string dataSetName,
        IReadOnlyCollection<string> fieldNames,
        DataSetConfigurationProvider dataSets,
        ICalculationConfigurationProvider calculations,
        IPipelineConfigurationProvider pipelines,
        ILogger logger,
        CancellationToken ct)
    {
        var fields = new HashSet<string>(fieldNames, StringComparer.OrdinalIgnoreCase);
        var consumers = fields.ToDictionary(f => f, _ => new List<FieldConsumerResponse>(), StringComparer.OrdinalIgnoreCase);

        var fromDataSets = await AddDataSetConsumers(dataSetName, fields, consumers, dataSets, ct).ConfigureAwait(false);
        if (fromDataSets.IsFailure)
            return fromDataSets.ToNewResult<FieldConsumerScan>();

        var fromCalculations = await AddCalculationConsumers(dataSetName, fields, consumers, calculations, ct).ConfigureAwait(false);
        if (fromCalculations.IsFailure)
            return fromCalculations.ToNewResult<FieldConsumerScan>();

        // A graph that cannot be read fails the scan too: an empty list must not hide a pipeline the read broke on.
        var graphs = await PipelineLineageLoader.ReadGraphs(pipelines, ct).ConfigureAwait(false);
        if (graphs.IsFailure)
            return graphs.ToNewResult<FieldConsumerScan>();

        var notFollowed = await GraphFieldConsumers.Add(
            dataSetName, fields, consumers, graphs.Value!, (name, token) => ReadFields(dataSets, name, token), logger, ct).ConfigureAwait(false);

        FieldConsumerScanLog.Scanned(logger, dataSetName, fields.Count, consumers.Values.Sum(c => c.Count));
        return GenericResult<FieldConsumerScan>.Success(new FieldConsumerScan(consumers, notFollowed));
    }

    // The fields of a data set the way a graph is described (FDW-1396): in the order the data set states them, the deleted ones left out.
    private static async Task<IGenericResult<IReadOnlyList<GraphField>>> ReadFields(DataSetConfigurationProvider dataSets, string name, CancellationToken ct)
    {
        var read = await dataSets.Get(name, ConfigurationLevels.Children, ct).ConfigureAwait(false);
        if (read.IsFailure || read.Value is null)
            return read.ToNewResult<IReadOnlyList<GraphField>>();

        return GenericResult<IReadOnlyList<GraphField>>.Success(
            [.. read.Value.Fields.Where(field => !field.IsDeleted).OrderBy(field => field.Ordinal).Select(field => new GraphField(field.Name, field.TypeName))]);
    }

    private static async Task<IGenericResult> AddDataSetConsumers(
        string dataSetName, HashSet<string> fields, Dictionary<string, List<FieldConsumerResponse>> consumers,
        DataSetConfigurationProvider dataSets, CancellationToken ct)
    {
        var all = await dataSets.Get(ct).ConfigureAwait(false);
        if (all.IsFailure)
            return all;

        foreach (var other in all.Value!.Where(d => !string.Equals(d.Name, dataSetName, StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var source in other.Sources.Where(s => string.Equals(s.SourceDataSetName, dataSetName, StringComparison.OrdinalIgnoreCase)))
            {
                foreach (var mapping in source.Mappings.Where(m => m.IsCurrent && !m.IsDeleted
                             && string.Equals(m.SourceKind, "DataSet", StringComparison.Ordinal)))
                {
                    var upstream = mapping.PhysicalFieldName is { Length: > 0 } named ? named : mapping.LogicalFieldName;
                    if (fields.Contains(upstream))
                        consumers[upstream].Add(new FieldConsumerResponse { ConsumerKind = "DataSet", ConsumerName = other.Name, ConsumerField = mapping.LogicalFieldName });
                }
            }
        }

        return GenericResult.Success();
    }

    private static async Task<IGenericResult> AddCalculationConsumers(
        string dataSetName, HashSet<string> fields, Dictionary<string, List<FieldConsumerResponse>> consumers,
        ICalculationConfigurationProvider calculations, CancellationToken ct)
    {
        // Each implementation holds its inputs and steps in its own tables, so they are read through
        // the composed calculations, never from one table.
        var all = await calculations.Get(ct).ConfigureAwait(false);
        if (all.IsFailure)
            return all;

        foreach (var calculation in all.Value!)
        {
            foreach (var input in calculation.Inputs.Where(i => string.Equals(i.DataSetName, dataSetName, StringComparison.OrdinalIgnoreCase)))
            {
                foreach (var step in calculation.Steps)
                {
                    foreach (var operand in step.Operands.Where(o =>
                                 string.Equals(o.InputAlias, input.InputAlias, StringComparison.Ordinal)
                                 && o.FieldName is { Length: > 0 } f && fields.Contains(f)))
                    {
                        consumers[operand.FieldName!].Add(new FieldConsumerResponse { ConsumerKind = "Calculation", ConsumerName = calculation.Name, ConsumerField = step.OutputAlias });
                    }
                }
            }
        }

        return GenericResult.Success();
    }
}
