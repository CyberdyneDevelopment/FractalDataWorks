using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Aegis.Abstractions;

/// <summary>
/// The outcome of evaluating an <see cref="ApprovalRequest"/>.
/// </summary>
/// <remarks>
/// Why: <see cref="Disposition"/> defaults to <see cref="VerdictDispositions.Deny"/> — a fail-closed
/// verdict, not an unset/null state. There is no "default = allow": an <see cref="IApprovalPolicyEvaluator"/>
/// that returns before explicitly setting a disposition, or a caller that constructs a bare
/// <see cref="Verdict"/>, gets a non-approving result rather than one that silently permits injection
/// (NO FALLBACKS — but this is a deliberate secure default, not a masked error).
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class Verdict
{
    /// <summary>
    /// Gets or sets the rendered disposition. Fail-closed default: <see cref="VerdictDispositions.Deny"/>.
    /// </summary>
    public IVerdictDisposition Disposition { get; set; } = VerdictDispositions.Deny;

    /// <summary>
    /// Gets or sets the identity of the actor who rendered this verdict (a human name, an agent
    /// identifier, or the policy evaluator's own name for a deterministic auto-deny), or
    /// <see langword="null"/> when no actor has rendered a verdict yet.
    /// </summary>
    public string? Actor { get; set; }

    /// <summary>
    /// Gets or sets the reason for this verdict, or <see langword="null"/> when none was given.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the correlation identifier of the <see cref="ApprovalRequest"/> this verdict
    /// answers.
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp this verdict was rendered.
    /// </summary>
    public DateTimeOffset DecidedAt { get; set; }
}
