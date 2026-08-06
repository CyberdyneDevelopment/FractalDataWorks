using System.Collections.Concurrent;
using System.Text.Json;
using Fdw.Results;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RootNamespace.ServiceName.Abstractions;

namespace RootNamespace.ServiceName;

/// <summary>
/// Default provider implementation for ServiceName services.
/// Acts as a mini-IoC container that stores factory instances and resolves services at runtime.
/// </summary>
/// <remarks>
/// <para>
/// This provider uses a two-phase registration pattern:
/// <list type="bullet">
/// <item><description>Phase 1: Each ServiceType registers infrastructure AND factory with main IServiceCollection</description></item>
/// <item><description>Phase 2: Each ServiceType resolves factory from DI and registers with this provider</description></item>
/// </list>
/// </para>
/// <para>
/// Configuration is loaded from header options with a value bag that is bound to the concrete
/// configuration type at runtime using ServiceType.ConfigurationType.
/// </para>
/// </remarks>
public class DefaultServiceNameProvider : ServiceProvider, IServiceNameProvider, IDisposable
{
    private readonly Dictionary<string, IServiceNameFactory> _factories = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, IServiceNameService> _cachedServices = new();
    private readonly ILogger<DefaultServiceNameProvider> _logger;
    private readonly IOptionsMonitor<ServiceNameHeaderOptions> _headerOptions;
    private readonly IDisposable? _changeSubscription;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultServiceNameProvider"/> class.
    /// </summary>
    public DefaultServiceNameProvider(
        ILogger<DefaultServiceNameProvider> logger,
        IOptionsMonitor<ServiceNameHeaderOptions> headerOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _headerOptions = headerOptions ?? throw new ArgumentNullException(nameof(headerOptions));

        // Subscribe to hot-reload
        _changeSubscription = headerOptions.OnChange(OnOptionsChanged);
    }

    /// <summary>
    /// Registers a factory instance for a named service type.
    /// Called by ServiceType.RegisterFactory() during Phase 2.
    /// </summary>
    /// <param name="name">The service type name (e.g., "ImplName").</param>
    /// <param name="factory">The factory instance resolved from DI.</param>
    public void RegisterFactory(string name, IServiceNameFactory factory)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));
        ArgumentNullException.ThrowIfNull(factory);

        _factories[name] = factory;
        _logger.LogDebug("Registered factory for ServiceName type: {Name}", name);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IServiceNameService>> GetServiceAsync(string name)
    {
        if (string.IsNullOrEmpty(name))
            return GenericResult<IServiceNameService>.Failure("Service name cannot be null or empty");

        // Check cache first
        if (_cachedServices.TryGetValue(name, out var cached))
            return GenericResult<IServiceNameService>.Success(cached);

        // Get header from IOptionsMonitor (supports hot-reload)
        if (!_headerOptions.CurrentValue.TryGetValue(name, out var header))
            return GenericResult<IServiceNameService>.Failure($"Service '{name}' not found in configuration");

        // Get ServiceType to determine configuration type
        var serviceType = ServiceNameTypes.ByName(header.ServiceNameType);
        if (serviceType == null || serviceType == ServiceNameTypes.Empty)
            return GenericResult<IServiceNameService>.Failure($"Unknown service type: {header.ServiceNameType}");

        // Bind value bag to concrete configuration type
        var config = BindValueBag(header.Configuration, serviceType.ConfigurationType);
        if (config == null)
            return GenericResult<IServiceNameService>.Failure($"Failed to bind configuration for type: {serviceType.ConfigurationType.Name}");

        // Set common properties from header
        config.Name = name;

        return await GetServiceAsync(config);
    }

    /// <inheritdoc/>
    public Task<IGenericResult<IServiceNameService>> GetServiceAsync(IServiceNameConfiguration configuration)
    {
        if (configuration == null)
            return Task.FromResult(GenericResult<IServiceNameService>.Failure("Configuration cannot be null"));

        // Derive type name from configuration class name
        var typeName = configuration.GetType().Name.Replace("ServiceNameConfiguration", "").Replace("Configuration", "");

        if (!_factories.TryGetValue(typeName, out var factory))
            return Task.FromResult(GenericResult<IServiceNameService>.Failure($"No factory registered for type: {typeName}"));

        try
        {
            var service = factory.Create(configuration);

            // Cache by configuration name if available
            var cacheKey = configuration.Name ?? typeName;
            _cachedServices.TryAdd(cacheKey, service);

            return Task.FromResult(GenericResult<IServiceNameService>.Success(service));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create ServiceName service for type: {TypeName}", typeName);
            return Task.FromResult(GenericResult<IServiceNameService>.Failure($"Failed to create service: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetAvailableServices()
    {
        return _factories.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// Binds a value bag dictionary to a concrete configuration type.
    /// </summary>
    private IServiceNameConfiguration? BindValueBag(Dictionary<string, object?> valueBag, Type configType)
    {
        try
        {
            var json = JsonSerializer.Serialize(valueBag);
            return JsonSerializer.Deserialize(json, configType) as IServiceNameConfiguration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bind value bag to {ConfigType}", configType.Name);
            return null;
        }
    }

    /// <summary>
    /// Called when header options change (hot-reload).
    /// </summary>
    private void OnOptionsChanged(ServiceNameHeaderOptions options, string? name)
    {
        _logger.LogDebug("ServiceName configuration changed, clearing cache");
        _cachedServices.Clear();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _changeSubscription?.Dispose();
    }
}

/// <summary>
/// Header options containing all named ServiceName configurations.
/// Bound from "Services:ServiceName" section in appsettings.json.
/// </summary>
/// <remarks>
/// <para>
/// This class extends Dictionary to allow direct JSON binding of named configurations.
/// Each entry contains a header with common metadata and a value bag for type-specific config.
/// </para>
/// <example>
/// appsettings.json:
/// <code>
/// {
///   "Services": {
///     "ServiceName": {
///       "MyService": {
///         "ServiceNameType": "ImplName",
///         "Configuration": {
///           "Setting1": "value1",
///           "Setting2": 42
///         }
///       }
///     }
///   }
/// }
/// </code>
/// </example>
/// </remarks>
public class ServiceNameHeaderOptions : Dictionary<string, ServiceNameHeader>
{
    /// <summary>
    /// Gets the configuration section name for binding.
    /// </summary>
    public const string SectionName = "Services:ServiceName";

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceNameHeaderOptions"/> class.
    /// </summary>
    public ServiceNameHeaderOptions() : base(StringComparer.OrdinalIgnoreCase)
    {
    }
}

/// <summary>
/// Header for a single ServiceName configuration entry.
/// Contains common metadata plus a value bag for type-specific configuration.
/// </summary>
public class ServiceNameHeader
{
    /// <summary>
    /// Gets or sets the service type discriminator (e.g., "ImplName").
    /// </summary>
    public string ServiceNameType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets type-specific configuration as a value bag.
    /// Bound to concrete type at runtime using ServiceType.ConfigurationType.
    /// </summary>
    public Dictionary<string, object?> Configuration { get; set; } = new();
}
