using System;
using Fdw.Configuration;

namespace Fdw.Services.SecretManagers.Abstractions;

/// <summary>
/// Marker interface for typed secret manager body configurations
/// (MsSqlSecretManagerConfiguration, EnvironmentVariableConfiguration, etc.).
/// Each typed body implements this interface directly without inheriting from
/// <c>SecretManagerConfiguration</c>.
/// </summary>
/// <remarks>
/// Secret manager bodies are persisted in their own tables (sec.MsSqlSecretManager,
/// sec.EnvironmentVariableSecretManager, etc.) and linked to the parent
/// <c>sec.SecretManager</c> row via a <c>SecretManagerId</c> foreign key property.
/// The parent <c>SecretManagerConfiguration</c>
/// carries an <c>ISecretManagerConfiguration? Configuration</c> property populated on the read path.
/// </remarks>
public interface ISecretManagerConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the FK to the parent SecretManager's logical Id.</summary>
    Guid SecretManagerId { get; set; }
}
