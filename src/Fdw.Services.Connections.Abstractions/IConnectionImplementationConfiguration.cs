using System;
using Fdw.Configuration;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Marker interface for typed connection body configurations (MsSqlConnectionConfiguration,
/// HttpConnectionConfiguration, etc.). Each typed body implements this interface directly
/// without inheriting from <c>IConnectionImplementationConfiguration</c>.
/// </summary>
/// <remarks>
/// Connection bodies are persisted in their own tables (conn.MsSqlConnection,
/// conn.HttpConnection, etc.) and linked to the parent <c>conn.Connection</c> row
/// via a <c>ConnectionId</c> foreign key property.
/// The properties here are the ones every connection implementation has, whatever it connects to.
/// </remarks>
public interface IConnectionImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the FK to <c>conn.Connection.Id</c>.</summary>
    Guid ConnectionId { get; set; }

    /// <summary>Gets or sets the human-readable description.</summary>
    string? Description { get; set; }

    /// <summary>Gets or sets the environment this connection belongs to.</summary>
    string? Environment { get; set; }

    /// <summary>Gets or sets whether this connection is health-checked.</summary>
    bool HealthCheckEnabled { get; set; }

    /// <summary>Gets or sets whether this connection is health-checked at startup.</summary>
    bool HealthCheckOnStartup { get; set; }

    /// <summary>Gets or sets how often this connection is health-checked, in seconds.</summary>
    int? HealthCheckIntervalSeconds { get; set; }

    /// <summary>Gets or sets whether schema discovery runs against this connection.</summary>
    bool DiscoveryEnabled { get; set; }
}
