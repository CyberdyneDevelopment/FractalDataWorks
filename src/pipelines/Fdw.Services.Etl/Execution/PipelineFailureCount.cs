using System.Collections.Generic;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// What counting a pipeline's recent runs found (FDW-3864): the failures in a row that count, which the pipeline's thresholds are judged against.
/// </summary>
/// <param name="Count">How many failures counted, newest first, before a success, the boundary or the end of the runs.</param>
/// <param name="Failures">The failures that counted, newest first, for the alert and the record to name.</param>
/// <param name="Ended">Whether the count ended at a success or at the boundary of the pipeline's last change, so older runs cannot add to it.</param>
public sealed record PipelineFailureCount(int Count, IReadOnlyList<PipelineRunOutcome> Failures, bool Ended);
