using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Aegis.Abstractions;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Aegis.Configuration;

/// <summary>
/// Typed body for a command whose approval policy is ad-hoc: every invocation requires a fresh
/// verdict rather than a standing pre-approval. Standalone typed-body POCO — mirrors
/// <c>MsSqlConnectionConfiguration</c>.
/// </summary>
/// <remarks>
/// Why: Phase 1's <c>PreApprovedPolicyEvaluator</c> denies every AdHoc command unconditionally
/// (design §11 "gate every action as auto-deny") — <see cref="RequiresFreshVerdict"/> exists now so
/// the schema shape is stable, but no Phase 0/1 evaluator reads it yet. The human-in-the-loop path
/// that actually renders a fresh verdict per invocation is Phase 2.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "AegisCommand", ServiceType = "AdHoc")]
public partial class AdHocCommandConfiguration : IApprovalPolicyConfiguration
{
    /// <summary>Gets or sets the name, set by the domain provider from the domain row.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    // ========================================
    // IGenericConfiguration — implementation identity
    // ========================================

    /// <summary>
    /// Gets or sets the unique identifier for this implementation row.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the domain record's durable id.
    /// </summary>
    public Guid AegisCommandId { get; set; }

    /// <summary>
    /// Gets or sets the declared connection this command runs against.
    /// </summary>
    public string ConnectionName { get; set; } = string.Empty;

    // ========================================
    // Approval-policy properties
    // ========================================

    /// <summary>
    /// Gets or sets the name of the secret manager backend that owns the secret this command may
    /// inject. A reference only — never the secret value.
    /// </summary>
    public string SecretManagerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the key name of the secret within <see cref="SecretManagerName"/>.
    /// </summary>
    public string SecretKeyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether every invocation of this command requires a fresh verdict (no standing
    /// pre-approval). Declared for schema stability; unread until Phase 2's human-in-the-loop path.
    /// </summary>
    public bool RequiresFreshVerdict { get; set; }
}
