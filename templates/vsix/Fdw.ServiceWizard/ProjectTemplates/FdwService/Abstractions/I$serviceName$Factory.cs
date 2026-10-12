using Fdw.Abstractions;

namespace $namespace$.$serviceName$.Abstractions;

/// <summary>
/// Factory interface for creating <see cref="I$serviceName$Service"/> instances.
/// </summary>
public interface I$serviceName$Factory : IServiceFactory<I$serviceName$Service, I$serviceName$Configuration>
{
    /// <summary>
    /// Creates a new service instance with the specified configuration.
    /// </summary>
    I$serviceName$Service Create(I$serviceName$Configuration configuration);
}
