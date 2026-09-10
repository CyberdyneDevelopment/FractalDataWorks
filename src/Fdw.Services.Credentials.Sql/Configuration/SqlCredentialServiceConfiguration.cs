using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Credentials.Abstractions;

namespace Fdw.Services.Credentials.Sql.Configuration;

/// <summary>
/// Typed body configuration for the SQL credential service implementation.
/// Persisted to <c>sec.SqlCredentialService</c> as a child of <c>sec.CredentialService</c>
/// via <see cref="CredentialServiceId"/>.
/// </summary>
/// <remarks>
/// <para>
/// Carries the credential policy that previously lived in the deleted
/// <c>AuthenticationSql</c> / <c>PersonalAccessToken</c> appsettings sections:
/// the credential vault name, the secret manager + HMAC key name for PAT hashing, the
/// environment segment, and the per-user token limit. Nothing credential-related remains in
/// appsettings — these values are runtime configuration rows inside ConfigurationDb.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "CredentialService", ServiceType = "Sql")]
public partial class SqlCredentialServiceConfiguration : ICredentialServiceImplementationConfiguration
{
    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    // ========================================
    // IGenericConfiguration — typed body identity
    // ========================================

    /// <summary>
    /// Gets or sets the unique identifier for this typed body row (sec.SqlCredentialService.Id).
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
    // ICredentialServiceImplementationConfiguration
    // ========================================

    /// <summary>
    /// Gets or sets the FK to <c>sec.CredentialService.Id</c> (the parent header row).
    /// </summary>
    public Guid CredentialServiceId { get; set; }

    // ========================================
    // Credential policy (was AuthenticationSql / PersonalAccessToken appsettings)
    // ========================================

    /// <summary>
    /// Gets or sets the name of the credential <c>IDataVault</c> that stores PAT and agent key
    /// secrets. Required — a missing value fails loud on first credential operation, no default.
    /// </summary>
    public string? CredentialVaultName { get; set; }

    /// <summary>
    /// Gets or sets the name of the secret manager that holds the HMAC-SHA-256 key for PAT hashing.
    /// Required — a missing value fails loud on first token hash/verify, no default.
    /// </summary>
    public string? SecretManagerName { get; set; }

    /// <summary>
    /// Gets or sets the secret key name within the secret manager that carries the HMAC-SHA-256 key.
    /// Required — a missing value fails loud on first token hash/verify, no default.
    /// </summary>
    public string? HmacKeySecretName { get; set; }

    /// <summary>
    /// Gets or sets the environment segment embedded in generated token values (e.g. <c>prod</c>, <c>dev</c>).
    /// Required — a missing value fails loud at PAT command construction, no default.
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of active personal access tokens allowed per user.
    /// </summary>
    public int MaxTokensPerUser { get; set; }
}
