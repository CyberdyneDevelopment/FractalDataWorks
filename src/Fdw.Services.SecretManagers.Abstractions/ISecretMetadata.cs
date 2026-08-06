using System;
using System.Collections.Generic;

namespace Fdw.Services.SecretManagers.Abstractions;

/// <summary>
/// Interface representing metadata information about a secret without exposing the secret value.
/// Provides access to secret properties and audit information for management operations.
/// </summary>
/// <remarks>
/// This interface allows systems to work with secret information without accessing
/// the actual secret values, supporting use cases like secret inventory, lifecycle
/// management, and audit reporting.
/// </remarks>
public interface ISecretMetadata
{
    /// <summary>
    /// Gets the unique identifier or key name for the secret.
    /// </summary>
    /// <value>The secret key or identifier.</value>
    string Key { get; }

    /// <summary>
    /// Gets the container, vault, or namespace containing this secret.
    /// </summary>
    /// <value>The container name, or null if not applicable.</value>
    string? Container { get; }

    /// <summary>
    /// Gets the current version identifier of the secret.
    /// </summary>
    /// <value>The version identifier, or null if versioning is not supported.</value>
    string? Version { get; }

    /// <summary>
    /// Gets the date and time when the secret was created.
    /// </summary>
    /// <value>The creation timestamp.</value>
    DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Gets the date and time when the secret was last modified.
    /// </summary>
    /// <value>The last modification timestamp.</value>
    DateTimeOffset ModifiedAt { get; }

    /// <summary>
    /// Gets the date and time when the secret expires.
    /// </summary>
    /// <value>The expiration timestamp, or null if the secret does not expire.</value>
    DateTimeOffset? ExpiresAt { get; }

    /// <summary>
    /// Gets the user or system that created the secret.
    /// </summary>
    /// <value>The creator identifier, or null if not available.</value>
    string? CreatedBy { get; }

    /// <summary>
    /// Gets the user or system that last modified the secret.
    /// </summary>
    /// <value>The modifier identifier, or null if not available.</value>
    string? ModifiedBy { get; }

    /// <summary>
    /// Gets a value indicating whether this secret has expired.
    /// </summary>
    /// <value><c>true</c> if the secret has expired; otherwise, <c>false</c>.</value>
    bool IsExpired { get; }

    /// <summary>
    /// Gets a value indicating whether this secret is enabled/active.
    /// </summary>
    /// <value><c>true</c> if the secret is enabled; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// Disabled secrets cannot be retrieved but their metadata remains accessible
    /// for management operations.
    /// </remarks>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets a value indicating whether this secret contains binary data.
    /// </summary>
    /// <value><c>true</c> if the secret contains binary data; otherwise, <c>false</c>.</value>
    bool IsBinary { get; }

    /// <summary>
    /// Gets the size of the secret data in bytes.
    /// </summary>
    /// <value>The size of the secret data.</value>
    /// <remarks>
    /// This provides information about the secret size without exposing the actual data.
    /// Useful for storage management and capacity planning.
    /// </remarks>
    long SizeInBytes { get; }

    /// <summary>
    /// Gets the tags associated with this secret.
    /// </summary>
    /// <value>A collection of tags for categorizing and organizing secrets.</value>
    /// <remarks>
    /// Tags enable flexible organization, search, and policy application across secrets.
    /// Common uses include environment labels, application identifiers, and compliance markers.
    /// </remarks>
    IReadOnlyCollection<string> Tags { get; }

    /// <summary>
    /// Gets additional metadata properties associated with the secret.
    /// </summary>
    /// <value>A dictionary of metadata key-value pairs.</value>
    /// <remarks>
    /// Metadata can include custom properties, policy information, compliance data,
    /// or other provider-specific attributes that don't fit into standard properties.
    /// </remarks>
    IReadOnlyDictionary<string, object> Properties { get; }

    /// <summary>
    /// Gets available versions of this secret.
    /// </summary>
    /// <value>A collection of version identifiers, or empty if versioning is not supported.</value>
    /// <remarks>
    /// This property allows enumeration of all available versions of a secret
    /// without accessing the secret values themselves.
    /// </remarks>
    IReadOnlyCollection<string> AvailableVersions { get; }

    /// <summary>
    /// Gets the access permissions or policy associated with this secret.
    /// </summary>
    /// <value>The access policy identifier, or null if not applicable.</value>
    /// <remarks>
    /// This property provides information about who can access or modify the secret
    /// without exposing sensitive policy details.
    /// </remarks>
    string? AccessPolicy { get; }

    /// <summary>
    /// Gets the encryption method or key identifier used to protect this secret.
    /// </summary>
    /// <value>The encryption method or key identifier, or null if not applicable.</value>
    /// <remarks>
    /// This provides transparency about the encryption approach without exposing
    /// sensitive encryption details or keys.
    /// </remarks>
    string? EncryptionMethod { get; }
}