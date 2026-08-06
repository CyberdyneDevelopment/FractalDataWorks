using System;
using Fdw.Configuration;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Marker interface for typed connection body configurations (MsSqlConnectionConfiguration,
/// HttpConnectionConfiguration, etc.). Each typed body implements this interface directly
/// without inheriting from <c>ConnectionConfiguration</c>.
/// </summary>
/// <remarks>
/// Connection bodies are persisted in their own tables (conn.MsSqlConnection,
/// conn.HttpConnection, etc.) and linked to the parent <c>conn.Connection</c> row
/// via a <c>ConnectionId</c> foreign key property.
/// The parent <c>ConnectionConfiguration</c>
/// carries an <c>IConnectionConfiguration? Configuration</c> property populated on the read path.
/// </remarks>
public interface IConnectionConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the FK to <c>conn.Connection.Id</c>.</summary>
    Guid ConnectionId { get; set; }
}
