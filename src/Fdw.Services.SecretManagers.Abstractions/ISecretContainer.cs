using System;
using System.Collections.Generic;

namespace Fdw.Services.SecretManagers.Abstractions;

/// <summary>
/// Interface representing a secret container (vault, store, namespace) within a secret provider.
/// Provides information about the container's properties, capabilities, and access controls.
/// </summary>
/// <remarks>
/// Secret containers organize secrets within a provider and may have different access policies,
/// encryption settings, or operational characteristics. This interface provides metadata
/// about containers without exposing the secrets they contain.
/// </remarks>
public interface ISecretContainer
{
    /// <summary>
    /// Gets the unique identifier for this container.
    /// </summary>
    /// <value>The container identifier.</value>
    string ContainerId { get; }

    /// <summary>
    /// Gets the display name of this container.
    /// </summary>
    /// <value>The container display name.</value>
    string Name { get; }

    /// <summary>
    /// Gets the type of this container.
    /// </summary>
    /// <value>The container type (e.g., "Vault", "SecretStore", "Mount").</value>
    /// <remarks>
    /// Container types vary by provider and define the capabilities and behavior
    /// of the container within the provider's architecture.
    /// </remarks>
    string ContainerType { get; }

    /// <summary>
    /// Gets the description of this container.
    /// </summary>
    /// <value>The container description, or null if not provided.</value>
    string? Description { get; }

    /// <summary>
    /// Gets the provider that owns this container.
    /// </summary>
    /// <value>The provider identifier.</value>
    string ProviderId { get; }

    /// <summary>
    /// Gets when this container was created.
    /// </summary>
    /// <value>The creation timestamp.</value>
    DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Gets when this container was last modified.
    /// </summary>
    /// <value>The last modification timestamp.</value>
    DateTimeOffset ModifiedAt { get; }

    /// <summary>
    /// Gets the user or system that created this container.
    /// </summary>
    /// <value>The creator identifier, or null if not available.</value>
    string? CreatedBy { get; }

    /// <summary>
    /// Gets the user or system that last modified this container.
    /// </summary>
    /// <value>The modifier identifier, or null if not available.</value>
    string? ModifiedBy { get; }

    /// <summary>
    /// Gets a value indicating whether this container is enabled and active.
    /// </summary>
    /// <value><c>true</c> if the container is enabled; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// Disabled containers cannot be used for secret operations but their
    /// metadata remains accessible for management purposes.
    /// </remarks>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets a value indicating whether this container supports secret versioning.
    /// </summary>
    /// <value><c>true</c> if versioning is supported; otherwise, <c>false</c>.</value>
    bool SupportsVersioning { get; }

    /// <summary>
    /// Gets a value indicating whether this container supports secret expiration.
    /// </summary>
    /// <value><c>true</c> if expiration is supported; otherwise, <c>false</c>.</value>
    bool SupportsExpiration { get; }

    /// <summary>
    /// Gets a value indicating whether this container supports binary secrets.
    /// </summary>
    /// <value><c>true</c> if binary secrets are supported; otherwise, <c>false</c>.</value>
    bool SupportsBinarySecrets { get; }

    /// <summary>
    /// Gets the maximum size limit for secrets in this container.
    /// </summary>
    /// <value>The maximum secret size in bytes, or null if no limit.</value>
    long? MaxSecretSize { get; }

    /// <summary>
    /// Gets the maximum number of secrets allowed in this container.
    /// </summary>
    /// <value>The maximum secret count, or null if no limit.</value>
    int? MaxSecretCount { get; }

    /// <summary>
    /// Gets the current number of secrets in this container.
    /// </summary>
    /// <value>The current secret count.</value>
    /// <remarks>
    /// This count may be approximate or cached depending on the provider
    /// implementation and performance considerations.
    /// </remarks>
    int CurrentSecretCount { get; }

    /// <summary>
    /// Gets the access permissions or policy associated with this container.
    /// </summary>
    /// <value>The access policy identifier, or null if not applicable.</value>
    /// <remarks>
    /// This property provides information about who can access or modify
    /// secrets in this container without exposing sensitive policy details.
    /// </remarks>
    string? AccessPolicy { get; }

    /// <summary>
    /// Gets the encryption method or key identifier used for this container.
    /// </summary>
    /// <value>The encryption method or key identifier, or null if not applicable.</value>
    /// <remarks>
    /// This provides information about the encryption approach without exposing
    /// sensitive encryption details or keys.
    /// </remarks>
    string? EncryptionMethod { get; }

    /// <summary>
    /// Gets the tags associated with this container.
    /// </summary>
    /// <value>A collection of tags for categorizing and organizing containers.</value>
    /// <remarks>
    /// Tags enable flexible organization, search, and policy application across containers.
    /// Common uses include environment labels, team identifiers, and compliance markers.
    /// </remarks>
    IReadOnlyCollection<string> Tags { get; }

    /// <summary>
    /// Gets additional metadata properties associated with this container.
    /// </summary>
    /// <value>A dictionary of metadata key-value pairs.</value>
    /// <remarks>
    /// Metadata can include custom properties, operational settings, compliance data,
    /// or other provider-specific attributes that don't fit into standard properties.
    /// </remarks>
    IReadOnlyDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Gets the supported operations for this container.
    /// </summary>
    /// <value>A collection of operation names supported by this container.</value>
    /// <remarks>
    /// Different containers may support different sets of operations based on
    /// their configuration, access policies, or provider capabilities.
    /// Common operations include "Get", "Set", "Delete", "List", "GetMetadata".
    /// </remarks>
    IReadOnlyCollection<string> SupportedOperations { get; }

    /// <summary>
    /// Gets usage statistics for this container.
    /// </summary>
    /// <value>Usage statistics, or null if not available.</value>
    ISecretContainerUsage? Usage { get; }
}