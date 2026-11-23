namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>What judging a call under an answer policy gave (FDW-6142).</summary>
/// <param name="PolicyName">The policy.</param>
/// <param name="CallName">The call.</param>
/// <param name="Outcome">What the call is judged to be.</param>
/// <param name="ExpiresAfterSeconds">How long a request under the policy stays open, which the policy states.</param>
public sealed record AnswerPolicyJudgment(string PolicyName, string CallName, IAnswerPolicyOutcome Outcome, int ExpiresAfterSeconds);
