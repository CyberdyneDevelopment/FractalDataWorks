using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions.Thresholds;

/// <summary>How many runs wait, on the whole server and for each pipeline that has some (FDW-3161).</summary>
/// <param name="Server">The waiting runs of the whole server.</param>
/// <param name="ByPipeline">The waiting runs of each pipeline that has any, by pipeline name.</param>
public sealed record WaitCounts(int Server, IReadOnlyDictionary<string, int> ByPipeline)
{
    /// <summary>Counts the waiting runs of the whole server, or of one pipeline.</summary>
    /// <param name="pipeline">The pipeline to count; none counts the whole server.</param>
    /// <returns>The waiting runs; a pipeline with none listed has none.</returns>
    public int Of(string? pipeline) => pipeline is null ? Server : ByPipeline.TryGetValue(pipeline, out var waiting) ? waiting : 0;
}
