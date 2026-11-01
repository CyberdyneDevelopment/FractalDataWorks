using Fdw.Services.Etl.Graphs;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// How many runs of one pipeline may be in flight at once, and what happens to a run past that (FDW-3117).
/// </summary>
/// <param name="Key">Names the pipeline; runs with the same key count together.</param>
/// <param name="Limit">The most runs of the pipeline that may be in flight at once; at least one.</param>
/// <param name="RefusesWhenFull">Whether a run triggered while <paramref name="Limit"/> runs of the pipeline are active or waiting is refused; when false it waits for its turn.</param>
/// <param name="Delay">When a run past the limit is held until a set time (FDW-3129), the delay or time of day; otherwise <see langword="null"/>, and the run is queued or refused.</param>
public sealed record PipelineRunCap(string Key, int Limit, bool RefusesWhenFull, PipelineDelayedStart? Delay = null);
