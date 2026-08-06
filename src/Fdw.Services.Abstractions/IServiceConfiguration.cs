using Fdw.Configuration;

namespace Fdw.Services.Abstractions;

/// <summary>
/// Base interface for service configurations.
/// Extends IGenericConfiguration which provides ServiceType and ServiceOptionType.
/// </summary>
public interface IServiceConfiguration : IGenericConfiguration
{
    // ServiceType and ServiceOptionType are inherited from IGenericConfiguration

    // /// <summary>
    // /// Gets the retry policy configuration.
    // /// </summary>
    // IRetryPolicyConfiguration? RetryPolicy { get; }

    /// <summary>
    /// Gets the timeout in milliseconds.
    /// </summary>
    int TimeoutMs { get; }
}
