using Fdw.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Provides framework-internal operational connections (e.g., the ConfigurationDb connection).
/// Marker interface distinguishing framework connections from user-defined connections.
/// </summary>
/// <remarks>
/// Framework components that need access to infrastructure connections (ConfigurationDb, etc.)
/// should depend on <see cref="IServiceConnectionProvider"/> rather than <see cref="IConnectionProvider"/>.
/// This prevents user-defined connections from accidentally being resolved for internal use
/// and makes the dependency intent explicit in DI registrations.
/// </remarks>
public interface IServiceConnectionProvider : IConnectionProvider
{
    /// <summary>
    /// Registers a framework connection under the specified name.
    /// Call this after the app is built (Phase 2) to seed the bootstrap connection.
    /// </summary>
    /// <param name="name">The logical name for the connection (case-insensitive).</param>
    /// <param name="connection">The pre-created connection instance.</param>
    void Register(string name, IGenericConnection connection);
}
