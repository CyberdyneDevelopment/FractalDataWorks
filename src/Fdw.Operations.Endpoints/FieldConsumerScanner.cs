using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Calculations;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Data;
using Fdw.Services.Etl;
using Fdw.Services.Pipelines;
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
/// <item><description><b>Pipeline</b>: a pipeline whose source is this data set. With field mappings,
/// only the fields it maps, against the destination field. With none, it moves every field.</description></item>
/// <item><description><b>Calculation</b>: a calculation step operand that reads this field through an
/// input bound to this data set, against the step's output alias.</description></item>
/// </list>
/// </para>
/// <para>
/// Any read that fails fails the scan. An empty consumer list must mean "nothing reads this field",
/// never "the read broke".
/// </para>
/// </remarks>
internal static class FieldConsumerScanner
{
    /// <summary>Consumers of each of <paramref name="fieldNames"/>, keyed by field name (case-insensitive).</summary>
    public static async Task<IGenericResult<Dictionary<string, List<FieldConsumerResponse>>>> Scan(
        string dataSetName,
        IReadOnlyCollection<string> fieldNames,
        DataSetConfigurationProvider dataSets,
        PipelineServiceConfigurationProvider pipelines,
        ICalculationConfigurationProvider calculations,
        ILogger logger,
        CancellationToken ct)
    {
        var fields = new HashSet<string>(fieldNames, StringComparer.OrdinalIgnoreCase);
        var consumers = fields.ToDictionary(f => f, _ => new List<FieldConsumerResponse>(), StringComparer.OrdinalIgnoreCase);

        var fromDataSets = await AddDataSetConsumers(dataSetName, fields, consumers, dataSets, ct).ConfigureAwait(false);
        if (fromDataSets.IsFailure)
            return fromDataSets.ToNewResult<Dictionary<string, List<FieldConsumerResponse>>>();

        var fromPipelines = await AddPipelineConsumers(dataSetName, fields, consumers, pipelines, logger, ct).ConfigureAwait(false);
        if (fromPipelines.IsFailure)
            return fromPipelines.ToNewResult<Dictionary<string, List<FieldConsumerResponse>>>();

        var fromCalculations = await AddCalculationConsumers(dataSetName, fields, consumers, calculations, ct).ConfigureAwait(false);
        if (fromCalculations.IsFailure)
            return fromCalculations.ToNewResult<Dictionary<string, List<FieldConsumerResponse>>>();

        FieldConsumerScanLog.Scanned(logger, dataSetName, fields.Count, consumers.Values.Sum(c => c.Count));
        return GenericResult<Dictionary<string, List<FieldConsumerResponse>>>.Success(consumers);
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

    // The transforms and their field mappings come from the composed pipeline: a mapping row hangs from
    // its transform by RowId and names no transform id, so it is reached through the aggregate.
    private static async Task<IGenericResult> AddPipelineConsumers(
        string dataSetName, HashSet<string> fields, Dictionary<string, List<FieldConsumerResponse>> consumers,
        PipelineServiceConfigurationProvider pipelines, ILogger logger, CancellationToken ct)
    {
        var configured = await pipelines.Get(ct).ConfigureAwait(false);
        if (configured.IsFailure)
            return configured;

        foreach (var pipeline in configured.Value!)
        {
            var record = PipelineLineageProjection.From(pipeline, logger);
            if (!string.Equals(record.SourceDataSet, dataSetName, StringComparison.OrdinalIgnoreCase))
                continue;

            var mapped = pipeline is IEtlPipelineImplementationConfiguration { Transforms: { } transforms }
                ? transforms.SelectMany(t => t.FieldMappings).Where(m => m.IsEnabled).ToList()
                : [];

            if (mapped.Count == 0)
            {
                // No field mappings: the pipeline moves every field through as it is.
                foreach (var field in fields)
                    consumers[field].Add(new FieldConsumerResponse { ConsumerKind = "Pipeline", ConsumerName = record.Name, ConsumerField = field });
                continue;
            }

            foreach (var mapping in mapped.Where(m => fields.Contains(m.SourceField)))
                consumers[mapping.SourceField].Add(new FieldConsumerResponse { ConsumerKind = "Pipeline", ConsumerName = record.Name, ConsumerField = mapping.DestinationField });
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
