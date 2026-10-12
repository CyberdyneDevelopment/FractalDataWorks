using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Aegis;

/// <summary>
/// The sanitized outcome of an <see cref="AegisInjector"/> injection. Carries only a success flag
/// and a downstream reference/fingerprint — never the resolved secret.
/// </summary>
/// <remarks>
/// Why: this is the one type that crosses back out of the injector's address space to
/// <c>AegisToolService</c> and, from there, to Claude. Its shape enforces non-exposure by
/// construction — there is no property here capable of carrying plaintext.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class AegisInjectionOutcome
{
    /// <summary>Gets or sets a value indicating whether the downstream injection succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the correlation identifier of the <see cref="Fdw.Aegis.Abstractions.ApprovalRequest"/>
    /// this outcome answers.
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets a sanitized downstream reference (e.g. the target's response body, a receipt
    /// id, a fingerprint) — never the secret itself.
    /// </summary>
    public string? Reference { get; set; }
}
