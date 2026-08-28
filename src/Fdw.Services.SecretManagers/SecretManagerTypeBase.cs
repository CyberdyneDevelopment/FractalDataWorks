using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Fdw.Configuration;
using Fdw.ServiceTypes;
using Fdw.Services.SecretManagers.Abstractions;

namespace Fdw.Services.SecretManagers;

/// <summary>
/// Base class for secret management service type definitions that inherit from ServiceTypeBase.
/// Provides secret management-specific metadata and capabilities.
/// </summary>
/// <typeparam name="TService">The secret management service type.</typeparam>
/// <typeparam name="TFactory">The factory type for creating secret management service instances.</typeparam>
/// <typeparam name="TConfiguration">The secret management configuration type.</typeparam>
/// <remarks>
/// Secret management types should inherit from this class and provide metadata only -
/// no instantiation logic should be included (that belongs in factories).
/// The ServiceTypeCollectionGenerator will discover all types inheriting from this base.
/// </remarks>
public abstract class SecretManagerTypeBase<TService, TFactory, TConfiguration> :
    ServiceTypeBase<TService, TFactory, TConfiguration>,
    ISecretManagerType<TService, TFactory, TConfiguration>
    where TService : ISecretManager
    where TFactory : ISecretManagerServiceFactory<TService, TConfiguration>
    where TConfiguration : class, IGenericConfiguration
{
    /// <summary>
    /// Gets the secret store types supported by this provider.
    /// </summary>
    /// <remarks>
    /// Examples: ["AzureKeyVault", "AzureAppConfiguration"] for Azure providers,
    /// ["HashiCorpVault"] for HashiCorp Vault,
    /// ["AWSSecretsManager", "AWSParameterStore"] for AWS providers.
    /// </remarks>
    public string[] SupportedSecretStores { get; }

    /// <summary>
    /// Gets the secret types supported by this provider.
    /// </summary>
    /// <remarks>
    /// Common secret types include:
    /// - "Password": Plain text passwords
    /// - "Certificate": X.509 certificates
    /// - "Key": Cryptographic keys
    /// - "ConnectionString": Database or service connection strings
    /// - "ApiKey": API keys and tokens
    /// </remarks>
    public IReadOnlyList<string> SupportedSecretTypes { get; }

    /// <summary>
    /// Gets a value indicating whether this provider supports secret rotation.
    /// </summary>
    /// <remarks>
    /// Secret rotation automatically updates secrets periodically or on-demand
    /// to maintain security. This is critical for compliance and security best practices.
    /// </remarks>
    public bool SupportsRotation { get; }

    /// <summary>
    /// Gets a value indicating whether this provider supports secret versioning.
    /// </summary>
    /// <remarks>
    /// Versioning allows maintaining multiple versions of a secret,
    /// enabling rollback and gradual migration scenarios.
    /// </remarks>
    public bool SupportsVersioning { get; }

    /// <summary>
    /// Gets a value indicating whether this provider supports bulk operations for retrieving or storing multiple secrets at once.
    /// </summary>
    public bool SupportsBulkOperations { get; }

    /// <summary>
    /// Gets a value indicating whether this provider supports encryption at rest for stored secrets.
    /// </summary>
    public bool SupportsEncryptionAtRest { get; }

    /// <summary>
    /// Gets a value indicating whether this provider supports auditing of secret access and modifications.
    /// </summary>
    public bool SupportsAuditing { get; }

    /// <summary>
    /// Gets the maximum size of a secret in bytes that this provider can store.
    /// </summary>
    public int MaxSecretSize { get; }

    /// <summary>
    /// Gets the cloud provider name (e.g., "Azure", "AWS", "Google Cloud") or "On-Premises" for local solutions.
    /// </summary>
    public string CloudProvider { get; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether this provider supports soft delete.
    /// </summary>
    /// <remarks>
    /// Soft delete allows recovery of deleted secrets within a retention period,
    /// preventing accidental permanent data loss.
    /// </remarks>
    public bool SupportsSoftDelete { get; }

    /// <summary>
    /// Gets a value indicating whether this provider supports access policies.
    /// </summary>
    /// <remarks>
    /// Access policies enable fine-grained control over who can read, write,
    /// or manage specific secrets.
    /// </remarks>
    public bool SupportsAccessPolicies { get; }

    /// <summary>
    /// Gets the maximum secret size in bytes supported by this provider.
    /// </summary>
    /// <remarks>
    /// Different secret stores have different size limitations.
    /// 0 indicates no limit, -1 indicates unknown limit.
    /// </remarks>
    public int MaxSecretSizeBytes { get; }

    /// <summary>
    /// Gets a value indicating whether this provider supports batch operations.
    /// </summary>
    /// <remarks>
    /// Batch operations allow retrieving or updating multiple secrets
    /// in a single API call, improving performance.
    /// </remarks>
    public bool SupportsBatchOperations { get; }

    /// <summary>
    /// Gets a value indicating whether this provider supports secret expiration.
    /// </summary>
    /// <remarks>
    /// Secret expiration automatically invalidates secrets after a specified time,
    /// forcing rotation and improving security.
    /// </remarks>
    public bool SupportsExpiration { get; }

    /// <summary>
    /// Gets a value indicating whether this provider supports secret tagging.
    /// </summary>
    /// <remarks>
    /// Tags allow organizing and categorizing secrets with metadata
    /// for easier management and discovery.
    /// </remarks>
    public bool SupportsTagging { get; }

    /// <summary>
    /// Gets the priority for provider selection when multiple providers are available.
    /// </summary>
    /// <remarks>
    /// Higher values indicate higher priority. When multiple secret management
    /// providers are configured, the system can use this to determine
    /// the preferred provider for automatic selection.
    /// </remarks>
    public int Priority { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretManagerTypeBase{TService, TConfiguration, TFactory}"/> class.
    /// </summary>
    /// <param name="name">The name of the secret management type.</param>
    /// <param name="sectionName">The configuration section name for appsettings.json.</param>
    /// <param name="displayName">The display name for this service type.</param>
    /// <param name="description">The description of what this service type provides.</param>
    /// <param name="supportedSecretStores">The secret store types supported by this provider.</param>
    /// <param name="supportedSecretTypes">The secret types supported by this provider.</param>
    /// <param name="supportsRotation">Indicates whether this provider supports secret rotation.</param>
    /// <param name="supportsVersioning">Indicates whether this provider supports secret versioning.</param>
    /// <param name="supportsSoftDelete">Indicates whether this provider supports soft delete.</param>
    /// <param name="supportsAccessPolicies">Indicates whether this provider supports access policies.</param>
    /// <param name="maxSecretSizeBytes">The maximum secret size in bytes supported by this provider.</param>
    /// <param name="supportsBatchOperations">Indicates whether this provider supports batch operations. Defaults to false.</param>
    /// <param name="supportsExpiration">Indicates whether this provider supports secret expiration. Defaults to false.</param>
    /// <param name="supportsTagging">Indicates whether this provider supports secret tagging. Defaults to false.</param>
    /// <param name="priority">The priority for provider selection. Higher values indicate higher priority. Defaults to 50.</param>
    /// <param name="category">The category for this secret management type (defaults to "Secret Management").</param>
    /// <param name="defaultContainerName">The default container name for this secret manager type.</param>
    protected SecretManagerTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string[] supportedSecretStores,
        IReadOnlyList<string> supportedSecretTypes,
        bool supportsRotation,
        bool supportsVersioning,
        bool supportsSoftDelete,
        bool supportsAccessPolicies,
        int maxSecretSizeBytes,
        bool supportsBatchOperations = false,
        bool supportsExpiration = false,
        bool supportsTagging = false,
        int priority = 50,
        string? category = null,
        string defaultContainerName = "")
        : base(name, sectionName, displayName, description, category ?? "Secret Management",
               defaultDataStoreName: "ConfigurationDb",
               defaultPathName: "sec",
               defaultContainerName: defaultContainerName)
    {
        SupportedSecretStores = supportedSecretStores ?? throw new ArgumentNullException(nameof(supportedSecretStores));
        SupportedSecretTypes = supportedSecretTypes ?? throw new ArgumentNullException(nameof(supportedSecretTypes));
        SupportsRotation = supportsRotation;
        SupportsVersioning = supportsVersioning;
        SupportsSoftDelete = supportsSoftDelete;
        SupportsAccessPolicies = supportsAccessPolicies;
        MaxSecretSizeBytes = maxSecretSizeBytes;
        SupportsBatchOperations = supportsBatchOperations;
        SupportsExpiration = supportsExpiration;
        SupportsTagging = supportsTagging;
        Priority = priority;
    }

}
