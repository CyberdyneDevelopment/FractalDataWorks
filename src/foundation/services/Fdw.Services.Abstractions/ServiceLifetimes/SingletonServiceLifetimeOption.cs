using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Abstractions;

/// <summary>
/// Singleton lifetime - single instance created once for the entire application lifetime.
/// </summary>
/// <remarks>
/// Use for expensive-to-create services or services that maintain global application state.
/// Most efficient but requires careful thread-safety considerations.
/// Instance lives for the entire application lifetime and is shared by all consumers.
/// </remarks>
public sealed class SingletonServiceLifetimeOption : ServiceLifetimeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SingletonServiceLifetimeOption"/> class.
    /// </summary>
    public SingletonServiceLifetimeOption() : base(3, "Singleton", "Single instance for application lifetime", ServiceLifetime.Singleton)
    {
    }
}