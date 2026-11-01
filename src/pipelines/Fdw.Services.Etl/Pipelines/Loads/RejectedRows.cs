using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Etl.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// Shapes the records a run's stages rejected into the rows a reject step writes (FDW-1142): each rejected record's own
/// fields plus the two reserved columns, <see cref="RejectedRecordColumns.Reason"/> and <see cref="RejectedRecordColumns.Stage"/>.
/// </summary>
/// <remarks>
/// Why a record that already has one of the two fields is refused: filling the column in would replace a value the record
/// carried, and nothing is replaced quietly. The run fails and the refusal names the field, the stage that rejected the
/// record and the pipeline.
/// </remarks>
public static class RejectedRows
{
    /// <summary>Makes the rows of the rejected records.</summary>
    /// <param name="pipelineName">The pipeline, for the refusal.</param>
    /// <param name="rejections">The records the stages rejected, in the order they were rejected.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>One row per rejected record, the records themselves unchanged; or a failure naming the first record that already has a reserved field.</returns>
    public static IGenericResult<IReadOnlyList<IDictionary<string, object?>>> Of(
        string pipelineName,
        IEnumerable<RejectedRecord> rejections,
        ILogger logger)
    {
        var rows = new List<IDictionary<string, object?>>();
        foreach (var rejected in rejections)
        {
            var carried = RejectedRecordColumns.All.FirstOrDefault(column => LoadRows.TryFind(rejected.Record, column, out _));
            if (carried is not null)
            {
                return GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Failure(
                    EtlResultCodes.ByName("RejectedRecordReservedColumn"),
                    logger,
                    ResultDetails.Create("StageName", rejected.Stage, "PipelineName", pipelineName, "FieldName", carried));
            }

            rows.Add(Row(rejected));
        }

        return GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Success(rows);
    }

    private static Dictionary<string, object?> Row(RejectedRecord rejected) =>
        new(rejected.Record, StringComparer.OrdinalIgnoreCase)
        {
            [RejectedRecordColumns.Reason] = rejected.Reason,
            [RejectedRecordColumns.Stage] = rejected.Stage,
        };
}
