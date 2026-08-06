using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Logging;

namespace Fdw.Services.SecretManagers.Sqlite.Configuration;

/// <summary>
/// Configuration for the SQLite secret management service.
/// Stores and retrieves secrets from a SQLite database file.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "SecretManager", ServiceType = "Sqlite")]
public sealed partial class SqliteSecretManagerConfiguration : ISecretManagerConfiguration
{
    private ILogger _logger = NullLogger<SqliteSecretManagerConfiguration>.Instance;

    // ========================================
    // ISecretManagerConfiguration — typed body identity
    // ========================================

    /// <summary>Gets or sets the logical identity of this configuration record.</summary>
    // Why: NO Guid.NewGuid() default — the provider mints this before INSERT via CreateVersion7().
    public Guid Id { get; set; }

    /// <summary>Gets or sets the FK to the parent SecretManager's logical Id.</summary>
    public Guid SecretManagerId { get; set; }

    // Why: Name/SectionName/ServiceType/ServiceOptionType live on the parent SecretManagerConfiguration
    // header row. Explicit interface members — typed body has no independent name.
    string IGenericConfiguration.Name { get => string.Empty; set { } }
    string IGenericConfiguration.SectionName => "SecretManagers";
    string IGenericConfiguration.ServiceType => "SecretManager";
    string? IGenericConfiguration.ServiceOptionType => "Sqlite";

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteSecretManagerConfiguration"/> class.
    /// </summary>
    public SqliteSecretManagerConfiguration()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteSecretManagerConfiguration"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for validation and diagnostic messages.</param>
    public SqliteSecretManagerConfiguration(ILogger<SqliteSecretManagerConfiguration>? logger = null)
    {
        _logger = logger ?? NullLogger<SqliteSecretManagerConfiguration>.Instance;
    }

    #region SQLite File Properties

    /// <summary>
    /// Gets or sets the path to the SQLite database file.
    /// </summary>
    /// <remarks>Accepts any path accepted by Microsoft.Data.Sqlite (e.g. absolute path, relative path, or ":memory:").</remarks>
    public string DataSource { get; set; } = string.Empty;

    #endregion

    #region Secret Table Properties

    /// <summary>
    /// Gets or sets the name of the secrets table within the SQLite file.
    /// </summary>
    /// <value>Default: "Secret"</value>
    public string TableName { get; set; } = "Secret";

    /// <summary>
    /// Gets or sets the command timeout in seconds for SQLite operations.
    /// </summary>
    /// <value>Default: 30 seconds</value>
    public int CommandTimeoutSeconds { get; set; } = 30;

    #endregion

    /// <summary>
    /// Gets the configuration name.
    /// </summary>
    public static string ConfigurationName => "Sqlite";

    /// <summary>
    /// Gets a value indicating whether this configuration is valid.
    /// </summary>
#pragma warning disable CA1822
    public bool IsValid => !string.IsNullOrWhiteSpace(DataSource);
#pragma warning restore CA1822

    /// <summary>
    /// Gets additional configuration properties as key-value pairs.
    /// </summary>
    public IReadOnlyDictionary<string, object> Properties => CreatePropertiesDictionary();

    private Dictionary<string, object> CreatePropertiesDictionary()
    {
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [nameof(DataSource)] = DataSource,
            [nameof(TableName)] = TableName,
            [nameof(CommandTimeoutSeconds)] = CommandTimeoutSeconds
        };
    }

    /// <inheritdoc/>
    public IGenericResult<ValidationResult> Validate()
    {
        var validator = new SqliteSecretManagerConfigurationValidator();
        var validationResult = validator.Validate(this);

        if (validationResult.IsValid)
        {
            return GenericResult<ValidationResult>.Success(validationResult);
        }

        var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
        return GenericResult<ValidationResult>.Failure(SecretManagerLogger.ValidationFailed(_logger, errors));
    }
}
