using Fdw.Results;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>How a data flow ended, with what it did up to then (FDW-1691).</summary>
/// <param name="Result">Success, or the first failure; a failure in one component stops the others.</param>
/// <param name="Report">
/// What moved and what each component did. Present whenever the flow started, failed or not, so a failed run can still say which
/// component failed and how far the rows got; absent only when the flow was refused before it started.
/// </param>
public sealed record FlowRunOutcome(IGenericResult Result, FlowRunReport? Report);
