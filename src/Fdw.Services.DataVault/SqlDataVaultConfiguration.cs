using System.ComponentModel.DataAnnotations.Schema;
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
/// by <c>DataVaultProvider</c>'s by-name cache-factory and handed to the ready vault.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "DataVault", ServiceType = "Default")]
public partial class SqlDataVaultConfiguration : IDataVaultImplementationConfiguration
{
    // ========================================
    // IGenericConfiguration — typed body identity
    // ========================================

    /// <summary>
    /// Gets or sets the unique identifier for this typed body row (sec.DefaultDataVault.Id).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name this configuration is resolved by.</summary>

    /// <summary>Gets or sets the name this configuration is resolved by.</summary>
    // Why it maps with no column of its own: the name is the domain's, and there is one
    // name for a configured member. The domain provider joins the domain row to this one,
    // and the join's result set is what carries it in.
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this is the current (active) version.
    /// </summary>
    public bool IsCurrent { get; set; }

    /// <summary>
    /// Gets or sets whether this typed body row has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    // ========================================
    // IDataVaultImplementationConfiguration
    // ========================================

    /// <summary>
    /// Gets or sets the FK to <c>sec.DataVault.Id</c> (the parent header row).
    /// </summary>
    public Guid DataVaultId { get; set; }

    // ========================================
    // Vault-specific properties (pointers only — never a secret)
    // ========================================

    /// <summary>
    /// Gets or sets the name of the configurationSchema-declared connection this vault rides.
    /// Resolved once in system context during vault resolution — never re-resolved per request.
    /// </summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the secret manager that holds the vault's pepper (HMAC key).
    /// </summary>
    public string SecretManagerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the secret key under which the pepper (HMAC key) is stored in the secret manager.
    /// The pepper itself is NEVER stored here — only the pointer to it.
    /// </summary>
    public string PepperSecretName { get; set; } = string.Empty;
}
