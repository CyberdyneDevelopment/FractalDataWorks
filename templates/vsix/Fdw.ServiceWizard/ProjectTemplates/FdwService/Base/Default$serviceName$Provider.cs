using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Configuration;
using Microsoft.Extensions.Logging;
using $namespace$.$serviceName$.Abstractions;

namespace $namespace$.$serviceName$;

/// <summary>
/// Default provider implementation for $serviceName$ services.
/// </summary>
/// <remarks>
/// Follows the service domain pattern where:
/// - Factories are registered as singletons with only ILogger dependencies
/// - Configuration is loaded from the DefaultConfigurationProvider (dual-source: ctrl+cfg)
/// - Services receive configuration directly at construction
/// </remarks>
// Why: Uses IServiceConfigurationProvider (implemented by DefaultConfigurationProvider) instead
// of raw IOptionsSnapshot to support the dual-source ctrl+cfg configuration model.
public class Default$serviceName$Provider : ServiceProvider, I$serviceName$Provider
{
    private readonly ConcurrentDictionary<string, I$serviceName$Factory> _factories = new();
    private readonly ConcurrentDictionary<string, I$serviceName$Service> _cachedServices = new();
    private readonly ILogger<Default$serviceName$Provider> _logger;
    private readonly IServiceConfigurationProvider<$serviceName$Configuration> _configurationProvider;

    public Default$serviceName$Provider(
        ILogger<Default$serviceName$Provider> logger,
        IServiceConfigurationProvider<$serviceName$Configuration> configurationProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configurationProvider = configurationProvider ?? throw new ArgumentNullException(nameof(configurationProvider));
    }

    public void RegisterFactory(string name, I$serviceName$Factory factory)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        _factories[name] = factory;
        _logger.LogDebug("Registered factory for $serviceName$ type: {Name}", name);
    }

    public Task<IGenericResult<I$serviceName$Service>> GetServiceAsync(string name)
    {
        if (string.IsNullOrEmpty(name))
            return Task.FromResult(GenericResult<I$serviceName$Service>.Failure("Service name cannot be null or empty"));

        if (_cachedServices.TryGetValue(name, out var cached))
            return Task.FromResult(GenericResult<I$serviceName$Service>.Success(cached));

        var configuration = _configurationProvider.Get(name);

        if (configuration == null)
            return Task.FromResult(GenericResult<I$serviceName$Service>.Failure($"Configuration not found for '{name}'"));

        return GetServiceAsync(configuration);
    }

    public Task<IGenericResult<I$serviceName$Service>> GetServiceAsync(I$serviceName$Configuration configuration)
    {
        if (configuration == null)
            return Task.FromResult(GenericResult<I$serviceName$Service>.Failure("Configuration cannot be null"));

        var typeName = configuration.Type;

        if (!_factories.TryGetValue(typeName, out var factory))
            return Task.FromResult(GenericResult<I$serviceName$Service>.Failure($"No factory registered for type: {typeName}"));

        try
        {
            var result = factory.Create(configuration);
            if (!result.IsSuccess)
                return Task.FromResult(result);

            var cacheKey = configuration.Name ?? typeName;
            _cachedServices.TryAdd(cacheKey, result.Value!);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create $serviceName$ service for type: {TypeName}", typeName);
            return Task.FromResult(GenericResult<I$serviceName$Service>.Failure($"Failed to create service: {ex.Message}"));
        }
    }

    public IReadOnlyList<string> GetAvailableServices()
    {
        return _factories.Keys.ToList().AsReadOnly();
    }
}
