using System;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>
/// The answer to a claim of a step with an outside effect (FDW-6139): the claim's id, whether this call made it, and the outcome the step recorded when it was claimed before.
/// </summary>
/// <param name="Id">The claim row's id, which the outcome is recorded against.</param>
/// <param name="IsNew">Whether this call made the claim; false means the step was claimed before and must not act again.</param>
/// <param name="ExecutionId">The run that made the claim.</param>
/// <param name="Outcome">The outcome recorded for a step claimed before (<see cref="StepClaimOutcomes"/>); <see langword="null"/> for a new claim and for a step claimed but never finished.</param>
public sealed record StepClaim(Guid Id, bool IsNew, Guid ExecutionId, string? Outcome);
