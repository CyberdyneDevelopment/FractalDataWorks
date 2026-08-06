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
    // ========================================
    // IGenericConfiguration — typed body identity
    // ========================================

    /// <summary>
    /// Gets or sets the unique identifier for this typed body row.
    /// </summary>
    // Why: NO Guid.NewGuid() default — DB owns identity assignment (strip-poco-defaults).
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the logical FK to the parent <c>AegisCommandConfiguration.Id</c>.
    /// </summary>
    public Guid AegisCommandId { get; set; }

    // Why: IGenericConfiguration members below satisfy the interface contract. Name/SectionName
    // are not meaningful on the typed body — the canonical name lives on the parent
    // AegisCommandConfiguration row. Mirrors MsSqlConnectionConfiguration.
    string IGenericConfiguration.Name
    {
        get => string.Empty;
        set { /* typed body has no independent name — it is identified by AegisCommandId */ }
    }

    string IGenericConfiguration.SectionName => "Commands";
    string IGenericConfiguration.ServiceType => "AegisCommand";
    string? IGenericConfiguration.ServiceOptionType => "AdHoc";

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
