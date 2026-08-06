using System;
using System.Collections.Generic;
using Fdw.Hosting.Abstractions.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Hosting.Abstractions.Builder;

/// <summary>
/// Builder for creating FDW hosts.
/// </summary>
public interface IFdwHostBuilder
{
    /// <summary>
    /// Gets a key/value collection for sharing data between components during building.
    /// </summary>
    IDictionary<object, object> Properties { get; }

    /// <summary>
    /// Configures host options.
    /// </summary>
    /// <param name="configure">The configuration delegate.</param>
    /// <returns>The builder for chaining.</returns>
    IFdwHostBuilder ConfigureHostOptions(Action<FdwHostOptions> configure);

    /// <summary>
    /// Configures host options using configuration binding.
    /// </summary>
    /// <param name="configure">The configuration delegate with context.</param>
    /// <returns>The builder for chaining.</returns>
    IFdwHostBuilder ConfigureHostOptions(Action<IFdwHostBuilderContext, FdwHostOptions> configure);

    /// <summary>
    /// Configures the host configuration.
    /// </summary>
    /// <param name="configure">The configuration delegate.</param>
    /// <returns>The builder for chaining.</returns>
    IFdwHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configure);

    /// <summary>
    /// Configures the application configuration.
    /// </summary>
    /// <param name="configure">The configuration delegate.</param>
    /// <returns>The builder for chaining.</returns>
    IFdwHostBuilder ConfigureAppConfiguration(Action<IFdwHostBuilderContext, IConfigurationBuilder> configure);

    /// <summary>
    /// Configures services for the host.
    /// </summary>
    /// <param name="configure">The configuration delegate.</param>
    /// <returns>The builder for chaining.</returns>
    IFdwHostBuilder ConfigureServices(Action<IServiceCollection> configure);

    /// <summary>
    /// Configures services for the host with context.
    /// </summary>
    /// <param name="configure">The configuration delegate.</param>
    /// <returns>The builder for chaining.</returns>
    IFdwHostBuilder ConfigureServices(Action<IFdwHostBuilderContext, IServiceCollection> configure);

    /// <summary>
    /// Configures logging.
    /// </summary>
    /// <param name="configure">The logging configuration delegate.</param>
    /// <returns>The builder for chaining.</returns>
    IFdwHostBuilder ConfigureLogging(Action<LoggingOptions> configure);

    /// <summary>
    /// Configures telemetry.
    /// </summary>
    /// <param name="configure">The telemetry configuration delegate.</param>
    /// <returns>The builder for chaining.</returns>
    IFdwHostBuilder ConfigureTelemetry(Action<TelemetryOptions> configure);

    /// <summary>
    /// Uses default configuration sources (appsettings.json, environment variables, etc.).
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>The builder for chaining.</returns>
    IFdwHostBuilder UseDefaultConfiguration(string[]? args = null);

    /// <summary>
    /// Sets the environment name.
    /// </summary>
    /// <param name="environment">The environment name.</param>
    /// <returns>The builder for chaining.</returns>
    IFdwHostBuilder UseEnvironment(string environment);

    /// <summary>
    /// Sets the content root path.
    /// </summary>
    /// <param name="contentRoot">The content root path.</param>
    /// <returns>The builder for chaining.</returns>
    IFdwHostBuilder UseContentRoot(string contentRoot);

    /// <summary>
    /// Builds the host.
    /// </summary>
    /// <returns>The built host.</returns>
    IFdwHost Build();
}
