using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Pipelines.Loads;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// The facts about a run that the engine declares in every graph pipeline (FDW-1982): <c>LastSuccessfulRun</c>, the start time of the pipeline's last
/// successful run, and <c>RunStarted</c>, the start time of this run. Both are read-only dates with a time and an offset, in scope for every step and
/// for the settings that are read when their step runs, and a pipeline cannot declare a parameter or variable with either name.
/// </summary>
/// <remarks>
/// Why they are two facts and not parameters the pipeline declares: a run's own start time and the stored watermark are not a caller's to state, and a
/// schedule, the API and a webhook must all see the same two values. The last run's <c>RunStarted</c> is the next run's <c>LastSuccessfulRun</c>, to the
/// tick, so consecutive windows neither overlap nor leave a gap.
/// </remarks>
public static class RunFacts
{
    /// <summary>The start time of the pipeline's last successful run; the run has no value for it when none is on record.</summary>
    public const string LastSuccessfulRun = PipelineRunParameterValues.LastSuccessfulRun;

    /// <summary>The start time of this run.</summary>
    public const string RunStarted = "RunStarted";

    /// <summary>Gets the names of the run facts.</summary>
    public static IReadOnlyList<string> Names { get; } = [LastSuccessfulRun, RunStarted];

    /// <summary>Whether a name is one of the run facts.</summary>
    /// <param name="name">The name.</param>
    /// <returns><see langword="true"/> for <c>LastSuccessfulRun</c> and <c>RunStarted</c>, whatever the case.</returns>
    public static bool IsFact(string name) => Names.Any(fact => string.Equals(fact, name, StringComparison.OrdinalIgnoreCase));
}
