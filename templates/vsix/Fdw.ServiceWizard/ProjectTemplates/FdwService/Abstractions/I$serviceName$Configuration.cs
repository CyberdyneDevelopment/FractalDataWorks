using Fdw.Configuration;

namespace $namespace$.$serviceName$.Abstractions;

/// <summary>
/// Configuration contract for $serviceName$ services.
/// </summary>
public interface I$serviceName$Configuration : IGenericConfiguration
{
    /// <summary>
    /// Gets the configuration section name for appsettings.json binding.
    /// </summary>
    static string SectionName => "Services:$serviceName$";
}
