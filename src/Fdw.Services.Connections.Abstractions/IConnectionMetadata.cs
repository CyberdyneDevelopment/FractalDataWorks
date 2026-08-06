using System;
using System.Collections.Generic;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Interface for connection metadata in the Fdw framework.
/// Provides information about the connected external system.
/// </summary>
/// <remarks>
/// Connection metadata helps the framework understand the capabilities and characteristics
/// of external systems, enabling better decision-making for operations like query optimization,
/// feature detection, and compatibility checks.
/// </remarks>
public interface IConnectionMetadata
{
    /// <summary>
    /// Gets the name of the external system or provider.
    /// </summary>
    /// <value>The system or provider name (e.g., "Microsoft SQL Server", "PostgreSQL", "REST API").</value>
    string SystemName { get; }

    /// <summary>
    /// Gets the version of the external system.
    /// </summary>
    /// <value>The version string of the external system, or null if not available.</value>
    string? Version { get; }

    /// <summary>
    /// Gets the server or endpoint information.
    /// </summary>
    /// <value>Server name, URL, or endpoint information (sanitized for security).</value>
    string? ServerInfo { get; }

    /// <summary>
    /// Gets the database or schema name, if applicable.
    /// </summary>
    /// <value>The database or schema name, or null if not applicable.</value>
    string? DatabaseName { get; }

    /// <summary>
    /// Gets additional system capabilities or features.
    /// </summary>
    /// <value>A dictionary of capability names and their availability or version information.</value>
    /// <remarks>
    /// This property provides extensible metadata about system-specific features,
    /// such as supported SQL features, API versions, or protocol capabilities.
    /// Keys should use consistent naming conventions (e.g., "SupportsTransactions", "MaxBatchSize").
    /// </remarks>
    IReadOnlyDictionary<string, object> Capabilities { get; }

    /// <summary>
    /// Gets the timestamp when this metadata was collected.
    /// </summary>
    /// <value>The UTC timestamp when metadata was last refreshed.</value>
    /// <remarks>
    /// This timestamp helps determine if metadata needs to be refreshed and provides
    /// context for troubleshooting connection issues that may be time-sensitive.
    /// </remarks>
    DateTimeOffset CollectedAt { get; }

    /// <summary>
    /// Gets custom properties specific to the connection type.
    /// </summary>
    /// <value>A dictionary of custom metadata properties.</value>
    /// <remarks>
    /// This property allows connection implementations to provide additional
    /// metadata that may be specific to their system type or use case.
    /// Common examples include connection pool information, authentication methods,
    /// or system-specific configuration details.
    /// </remarks>
    IReadOnlyDictionary<string, object> CustomProperties { get; }
}
