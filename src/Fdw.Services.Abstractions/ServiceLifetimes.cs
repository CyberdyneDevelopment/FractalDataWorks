namespace Fdw.Services.Abstractions;

/// <summary>
/// Static collection of service lifetimes.
/// Manually created to avoid circular dependencies with ServiceTypes.
/// </summary>
public static class ServiceLifetimes
{
    /// <summary>
    /// Transient lifetime - new instance created each time the service is requested.
    /// </summary>
    public static IServiceLifetime Transient { get; } = new TransientServiceLifetimeOption();

    /// <summary>
    /// Scoped lifetime - new instance created once per scope (typically per request).
    /// </summary>
    public static IServiceLifetime Scoped { get; } = new ScopedServiceLifetimeOption();

    /// <summary>
    /// Singleton lifetime - single instance created once for the entire application lifetime.
    /// </summary>
    public static IServiceLifetime Singleton { get; } = new SingletonServiceLifetimeOption();

    /// <summary>
    /// Gets a service lifetime by name.
    /// </summary>
    /// <param name="name">The name of the lifetime (case-insensitive).</param>
    /// <returns>The matching service lifetime, or null if not found.</returns>
    public static IServiceLifetime? ByName(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        return name!.ToLowerInvariant() switch
        {
            "transient" => Transient,
            "scoped" => Scoped,
            "singleton" => Singleton,
            _ => null
        };
    }
}