using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Aegis.Abstractions;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Aegis.Configuration;

/// <summary>
/// Typed body for a command whose approval policy is pre-approved: it names the secret it may
/// inject (by reference) and declares the exact parameter allow-list Claude's submitted parameters
/// must satisfy. Standalone typed-body POCO — mirrors <c>MsSqlConnectionConfiguration</c>.
/// </summary>
/// <remarks>
/// Persisted (Phase 2) to its own table as a child of the <c>AegisCommand</c> header via
/// <see cref="AegisCommandId"/>. For Phase 0/1 this is bound directly from <c>aegisSchema.json</c>
/// via <c>IOptions</c> — no ConfigurationDb round trip.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "AegisCommand", ServiceType = "PreApproved")]
public partial class PreApprovedCommandConfiguration : IApprovalPolicyConfiguration
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
    string? IGenericConfiguration.ServiceOptionType => "PreApproved";

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
    /// Gets or sets the deterministic parameter allow-list: every parameter this command accepts,
    /// the values it permits, and whether it is required. A submitted parameter absent from this
    /// list, or with a value not in <see cref="ParameterAllowEntry.PermittedValues"/>, is rejected.
    /// </summary>
    public IList<ParameterAllowEntry> ParameterAllowList { get; set; } = new List<ParameterAllowEntry>();
}
