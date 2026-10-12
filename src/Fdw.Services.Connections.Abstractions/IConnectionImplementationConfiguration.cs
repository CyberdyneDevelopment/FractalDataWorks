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

    /// <summary>Gets or sets the human-readable description.</summary>
    string? Description { get; set; }

    /// <summary>Gets when the implementation's row for this version of the connection was written.</summary>
    /// <remarks>
    /// A column of the implementation's own row, read like the connection's other settings. A save writes a new version of the
    /// row, so this is the time of the last save and a caller can tell a connection it opened has since been saved again.
    /// </remarks>
    DateTimeOffset CreateDate { get; }

    /// <summary>Gets when the implementation's row for this version of the connection was last modified.</summary>
    DateTimeOffset ModifyDate { get; }

    /// <summary>Gets or sets the environment this connection belongs to.</summary>
    string? Environment { get; set; }

    /// <summary>Gets or sets whether this connection is health-checked.</summary>
    bool HealthCheckEnabled { get; set; }

    /// <summary>Gets or sets whether this connection is health-checked at startup.</summary>
    bool HealthCheckOnStartup { get; set; }

    /// <summary>Gets or sets how often this connection is health-checked, in seconds.</summary>
    int? HealthCheckIntervalSeconds { get; set; }

    /// <summary>
    /// Gets or sets the name of the schema discovery option this connection uses: one of its connection
    /// type's <see cref="IConnectionType.SupportedDiscoveryTypes"/>, <c>None</c> when it does not discover.
    /// </summary>
    /// <remarks>
    /// Why a named option rather than an on/off flag: a flag could say "discover" on a connection whose
    /// kind cannot, and could not say which strategy to use for a kind that has several. There is no
    /// default; whoever creates the connection states it.
    /// </remarks>
    string SchemaDiscovery { get; set; }
}
