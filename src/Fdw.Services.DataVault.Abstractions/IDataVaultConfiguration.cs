using System;
using Fdw.Configuration;

namespace Fdw.Services.DataVault.Abstractions;

/// <summary>
/// Marker interface for typed data vault body configurations (DefaultDataVaultConfiguration,
/// etc.). Each typed body implements this interface directly without inheriting from the
/// parent <c>DataVaultConfiguration</c> header.
/// </summary>
/// <remarks>
/// Vault bodies are persisted in their own tables (sec.DefaultDataVault, etc.) and linked to
/// the parent <c>sec.DataVault</c> row via a <c>DataVaultId</c> foreign key property. The
/// parent header carries an <c>IDataVaultConfiguration? Configuration</c> property populated
/// on the read path.
/// </remarks>
public interface IDataVaultConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the FK to <c>sec.DataVault.Id</c>.</summary>
    Guid DataVaultId { get; set; }

    // Why: every vault rides ONE connection and has ONE pepper — these three pointers are
    // universal vault configuration, so they live on the marker interface. The provider reads
    // them generically (it never knows a concrete typed body) to resolve the connection + pepper
    // once in system context. They are pointers ONLY — never the secret itself.

    /// <summary>Gets or sets the name of the configurationSchema-declared connection the vault rides.</summary>
    string ConnectionName { get; set; }

    /// <summary>Gets or sets the name of the secret manager that holds the vault's pepper (HMAC key).</summary>
    string SecretManagerName { get; set; }

    /// <summary>Gets or sets the secret key under which the pepper is stored in the secret manager.</summary>
    string PepperSecretName { get; set; }
}
