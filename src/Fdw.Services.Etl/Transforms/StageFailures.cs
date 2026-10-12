using Fdw.Results;
using Fdw.Services.Etl.Results;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Tells the failure of a stage that stops the run from the failure of a stage that a pipeline set to continue on error may step past.
/// </summary>
/// <remarks>
/// Why this exists: a pipeline's continue-on-error setting is for a stage that could not do its work on a record. A Validate
/// stage whose failing action is Fail has done its work and found the record unfit, and the pipeline author asked for the run
/// to stop; stepping past it would hand the unfit record to the next stage and load it. The stage says so through its result
/// code, so every place that folds a stage failure asks here and none of them decides on its own.
/// </remarks>
public static class StageFailures
{
    /// <summary>
    /// Gets whether <paramref name="failure"/> ends the run even when the pipeline continues on error.
    /// </summary>
    /// <param name="failure">The failed result of a stage.</param>
    /// <returns>True when the stage failed because a record failed a check whose failing action stops the run.</returns>
    public static bool StopsRun(IGenericResult failure) =>
        failure.Code is { } code && code.Id == EtlResultCodes.ByName("RecordFailedValidation").Id;
}
