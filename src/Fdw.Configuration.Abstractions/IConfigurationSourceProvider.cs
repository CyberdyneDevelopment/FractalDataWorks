using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Configuration.Abstractions;

/// <summary>
/// Provides configuration sources for database-backed configuration.
/// </summary>
/// <remarks>
/// This abstraction allows ServiceTypeCollections to add configuration sources
/// without directly referencing the concrete implementation (e.g., MsSql).
/// </remarks>
public interface IConfigurationSourceProvider
{
    /// <summary>
    /// Adds a configuration source to the configuration manager.
    /// </summary>
    /// <param name="configurationManager">The configuration manager to add the source to.
    /// This is the IConfigurationManager returned by IHostApplicationBuilder.Configuration,
    /// which implements both IConfigurationBuilder and IConfiguration and auto-synchronizes.</param>
    /// <param name="serviceCategory">The service category filter (e.g., "Connection", "DataStore").</param>
    /// <param name="loggerFactory">Optional logger factory for startup logging.</param>
    void AddSource(
        IConfigurationManager configurationManager,
        string serviceCategory,
        ILoggerFactory? loggerFactory = null);
}
