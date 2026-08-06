using System.Collections.Generic;
using Fdw.Hosting.Abstractions.Configuration;
using Microsoft.Extensions.Configuration;

namespace Fdw.Hosting.Abstractions.Builder;

/// <summary>
/// Context available during host building.
/// Provides access to configuration and properties for modules.
/// </summary>
public interface IFdwHostBuilderContext
{
    /// <summary>
    /// Gets the configuration for the host being built.
    /// </summary>
    IConfiguration Configuration { get; }

    /// <summary>
    /// Gets the bound host options.
    /// </summary>
    FdwHostOptions HostOptions { get; }

    /// <summary>
    /// Gets the environment name.
    /// </summary>
    string EnvironmentName { get; }

    /// <summary>
    /// Gets the application name.
    /// </summary>
    string ApplicationName { get; }

    /// <summary>
    /// Gets the content root path.
    /// </summary>
    string ContentRootPath { get; }

    /// <summary>
    /// Gets a key/value collection for sharing data between modules during building.
    /// </summary>
    IDictionary<object, object> Properties { get; }

    /// <summary>
    /// Gets a value indicating whether the host is running in development environment.
    /// </summary>
    bool IsDevelopment { get; }

    /// <summary>
    /// Gets a value indicating whether the host is running in staging environment.
    /// </summary>
    bool IsStaging { get; }

    /// <summary>
    /// Gets a value indicating whether the host is running in production environment.
    /// </summary>
    bool IsProduction { get; }

    /// <summary>
    /// Checks if the current environment matches the specified name.
    /// </summary>
    /// <param name="environmentName">The environment name to check.</param>
    /// <returns>True if the environment matches.</returns>
    bool IsEnvironment(string environmentName);
}
