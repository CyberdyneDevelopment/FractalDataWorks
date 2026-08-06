using Fdw;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Abstractions.Commands;

/// <summary>
/// Command interface for creating connections.
/// </summary>
public interface IConnectionCreateCommand : IConnectionCommand
{
    /// <summary>
    /// Gets the name for the new connection.
    /// </summary>
    string ConnectionName { get; }

    /// <summary>
    /// Gets the provider type for the connection (e.g., "MsSql", "PostgreSQL").
    /// </summary>
    string ProviderType { get; }

    /// <summary>
    /// Gets the configuration for the connection.
    /// </summary>
    IGenericConfiguration ConnectionConfiguration { get; }
}
