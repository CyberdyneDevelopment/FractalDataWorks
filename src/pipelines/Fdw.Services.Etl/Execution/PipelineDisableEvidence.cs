using System.Collections.Generic;

namespace Fdw.Services.Etl.Execution;

/// <summary>What was recorded when the failure rule disabled a pipeline (FDW-3864).</summary>
/// <param name="ConsecutiveFailures">How many failures in a row counted when the rule fired.</param>
/// <param name="Reason">Why the rule fired, in the evaluator's words (the criteria that held).</param>
/// <param name="Failures">The failures that counted, newest first.</param>
public sealed record PipelineDisableEvidence(int ConsecutiveFailures, string Reason, IReadOnlyList<PipelineRunOutcome> Failures);
