using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Abstractions.OptionTypes;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// Picks a pipeline's load rows by the phase they run in (FDW-1142). The load rows of a pipeline are one list, each row
/// stating its phase; every reader that wants only the rows that load the output, or only the reject steps, asks here, so
/// none repeats the comparison.
/// </summary>
public static class LoadPhaseRows
{
    /// <summary>Gets the rows that load the pipeline's output: its own load and its load steps.</summary>
    /// <param name="rows">The pipeline's load rows, or <see langword="null"/> when it has none.</param>
    /// <returns>The rows whose phase is Load.</returns>
    public static IList<PipelineLoadStepConfiguration> Loading(IEnumerable<PipelineLoadStepConfiguration>? rows) => Of(rows, LoadPhases.Load);

    /// <summary>Gets the reject steps: the rows that write the records the pipeline's stages rejected.</summary>
    /// <param name="rows">The pipeline's load rows, or <see langword="null"/> when it has none.</param>
    /// <returns>The rows whose phase is Reject.</returns>
    public static IList<PipelineLoadStepConfiguration> Rejecting(IEnumerable<PipelineLoadStepConfiguration>? rows) => Of(rows, LoadPhases.Reject);

    private static List<PipelineLoadStepConfiguration> Of(IEnumerable<PipelineLoadStepConfiguration>? rows, ILoadPhase phase) =>
        rows is null
            ? []
            : [.. rows.Where(row => string.Equals(row.Phase, phase.Name, StringComparison.OrdinalIgnoreCase))];
}
