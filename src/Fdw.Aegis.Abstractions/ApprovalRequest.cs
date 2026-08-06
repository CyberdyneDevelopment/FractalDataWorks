using System;
using System.Collections.Generic;

namespace Fdw.Aegis.Abstractions;

/// <summary>
/// The deterministic ask submitted to an <see cref="IApprovalPolicyEvaluator"/>. Mirrors the shape
/// Claude submits to the Aegis <c>request_action</c> tool and that <c>IExecutionTracker.CreateItem</c>
/// will later persist (Phase 2) — same <see cref="CorrelationId"/> and <see cref="Parameters"/> dict
/// shape as <c>IExecutionTracker.CreateItem(parameters)</c>.
/// </summary>
/// <remarks>
/// Why: the secret is referenced by name (<see cref="SecretManagerName"/>/<see cref="SecretKeyName"/>),
/// never carried as a value — the FDW house rule (config carries a reference, never plaintext). Claude
/// never sees this class; it is constructed by <c>Aegis.McpServer</c> from the tool call arguments.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ApprovalRequest
{
    /// <summary>
    /// Gets or sets the correlation identifier that ties this request to its verdict, audit trail,
    /// and (Phase 2) <c>IExecutionTracker</c> item.
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the name of the declared connection this request targets (a
    /// <c>ConfigurationSchema.Commands</c> entry's <c>ConnectionName</c>).
    /// </summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the declared command this request invokes.
    /// </summary>
    public string CommandName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exact parameters submitted with the request — never mutated between
    /// submission and evaluation, so what a human/policy reviews is exactly what would execute.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Parameters { get; set; } = new Dictionary<string, object?>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the name of the secret manager backend that owns the referenced secret, or
    /// <see langword="null"/> when this command requires no secret. A reference only — never the
    /// secret value itself.
    /// </summary>
    public string? SecretManagerName { get; set; }

    /// <summary>
    /// Gets or sets the key name of the referenced secret within <see cref="SecretManagerName"/>, or
    /// <see langword="null"/> when this command requires no secret.
    /// </summary>
    public string? SecretKeyName { get; set; }

    /// <summary>
    /// Gets or sets the timestamp the request was submitted.
    /// </summary>
    public DateTimeOffset RequestedAt { get; set; }
}
