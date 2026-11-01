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
/// The PreApproved implementation of the AegisCommand domain: its row hangs from the domain row by
/// <see cref="AegisCommandId"/>, and its <see cref="ParameterAllowList"/> rows hang from it and load
/// and save with it.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "AegisCommand", ServiceType = "PreApproved")]
public partial class PreApprovedCommandConfiguration : IApprovalPolicyConfiguration
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
    /// Gets or sets the deterministic parameter allow-list: every parameter this command accepts,
    /// the values it permits, and whether it is required. A submitted parameter absent from this
    /// list, or with a value not in <see cref="ParameterAllowEntryConfiguration.PermittedValues"/>, is rejected.
    /// </summary>
    public IList<ParameterAllowEntryConfiguration> ParameterAllowList { get; set; } = new List<ParameterAllowEntryConfiguration>();
}
