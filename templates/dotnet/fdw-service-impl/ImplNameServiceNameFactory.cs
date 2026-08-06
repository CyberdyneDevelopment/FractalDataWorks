using Microsoft.Extensions.Logging;
using RootNamespace.ServiceName.Abstractions;

namespace RootNamespace.ServiceName.ImplName;

/// <summary>
/// Factory for creating <see cref="ImplNameServiceNameService"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// This factory is registered as a singleton in DI (Phase 1) and resolved in Phase 2.
/// Dependencies are injected via constructor by the DI container.
/// </para>
/// <para>
/// Factories are stateless singletons. Dependencies come from DI, configuration
/// is passed at runtime to the Create() method.
/// </para>
/// </remarks>
public sealed class ImplNameServiceNameFactory : IImplNameServiceNameFactory
{
    private readonly ILogger<ImplNameServiceNameFactory> _logger;
    private readonly ILogger<ImplNameServiceNameService> _serviceLogger;

    // TODO: Add any additional dependencies needed to create services
    // Example: private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImplNameServiceNameFactory"/> class.
    /// </summary>
    /// <param name="logger">The logger for the factory.</param>
    /// <param name="serviceLogger">The logger for created service instances.</param>
    public ImplNameServiceNameFactory(
        ILogger<ImplNameServiceNameFactory> logger,
        ILogger<ImplNameServiceNameService> serviceLogger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceLogger = serviceLogger ?? throw new ArgumentNullException(nameof(serviceLogger));
    }

    /// <inheritdoc/>
    public IServiceNameService Create(IServiceNameConfiguration configuration)
    {
        if (configuration is not ImplNameServiceNameConfiguration implConfig)
        {
            throw new ArgumentException(
                $"Configuration must be of type {nameof(ImplNameServiceNameConfiguration)}, " +
                $"but received {configuration.GetType().Name}",
                nameof(configuration));
        }

        _logger.LogDebug("Creating ImplNameServiceNameService for {Name}", configuration.Name);

        return new ImplNameServiceNameService(
            _serviceLogger,
            implConfig);
    }
}
