using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.DataVault.Abstractions;

namespace Fdw.Services.DataVault;

/// <summary>
/// Typed body configuration for the default data vault implementation.
/// Persisted to <c>sec.DefaultDataVault</c> as a child of <c>sec.DataVault</c>
/// via <see cref="DataVaultId"/>.
/// </summary>
/// <remarks>
/// <para>
/// Carries pointers only (never a secret): the <see cref="ConnectionName"/> the vault rides, the
/// <see cref="SecretManagerName"/> that holds the pepper, and the <see cref="PepperSecretName"/>.
/// The connection and pepper are resolved once in system context (never re-resolved per request)
/// by <c>DefaultDataVaultProvider</c>'s by-name cache-factory and handed to the ready vault.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "DataVault", ServiceType = "Default")]
public partial class DefaultDataVaultConfiguration : IDataVaultConfiguration
{
    // ========================================
    // IGenericConfiguration — typed body identity
    // ========================================

    /// <summary>
    /// Gets or sets the unique identifier for this typed body row (sec.DefaultDataVault.Id).
    /// </summary>
    // Why: NO Guid.NewGuid() default — the provider mints this before INSERT.
    // A random default would bypass the provider's Id-mint logic and create orphaned rows.
    public Guid Id { get; set; }


    /// <summary>
    /// Gets or sets whether this is the current (active) version.
    /// </summary>
    public bool IsCurrent { get; set; }

    /// <summary>
    /// Gets or sets whether this typed body row has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    // Why: IGenericConfiguration members below satisfy the interface contract.
    // Name, SectionName, ServiceType, ServiceOptionType are not meaningful on the typed
    // body — the canonical identity lives on the parent DataVaultConfiguration header.
    string IGenericConfiguration.Name
    {
        get => string.Empty;
        set { /* typed body has no independent name — it is identified by DataVaultId */ }
    }

    string IGenericConfiguration.SectionName => "DataVaults";
    string IGenericConfiguration.ServiceType => "DataVault";
    string? IGenericConfiguration.ServiceOptionType => "Default";

    // ========================================
    // IDataVaultConfiguration
    // ========================================

    /// <summary>
    /// Gets or sets the FK to <c>sec.DataVault.Id</c> (the parent header row).
    /// </summary>
    // Why: DataVaultId links this typed body back to its sec.DataVault parent row.
    // No Guid.NewGuid() default — the caller must explicitly supply the parent's Id.
    public Guid DataVaultId { get; set; }

    // ========================================
    // Vault-specific properties (pointers only — never a secret)
    // ========================================

    /// <summary>
    /// Gets or sets the name of the configurationSchema-declared connection this vault rides.
    /// Resolved once in system context during vault resolution — never re-resolved per request.
    /// </summary>
    // Why: NO default — a missing ConnectionName is a configuration error that fails loud during
    // vault resolution (DefaultDataVaultProvider), never a silent fallback.
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the secret manager that holds the vault's pepper (HMAC key).
    /// </summary>
    // Why: NO default — a missing SecretManagerName fails loud during vault resolution.
    public string SecretManagerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the secret key under which the pepper (HMAC key) is stored in the secret manager.
    /// The pepper itself is NEVER stored here — only the pointer to it.
    /// </summary>
    // Why: NO default — a missing PepperSecretName fails loud during vault resolution.
    public string PepperSecretName { get; set; } = string.Empty;
}
