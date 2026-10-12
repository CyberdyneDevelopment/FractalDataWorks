using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Abstractions;

/// <summary>
/// Transient lifetime - new instance created each time the service is requested.
/// </summary>
/// <remarks>
/// Use for lightweight, stateless services that don't hold resources.
/// Most expensive in terms of object creation but safest for concurrency.
/// Each consumer gets their own instance with no shared state.
/// </remarks>
public sealed class TransientServiceLifetimeOption : ServiceLifetimeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransientServiceLifetimeOption"/> class.
    /// </summary>
    public TransientServiceLifetimeOption() : base(1, "Transient", "New instance created each time", ServiceLifetime.Transient)
    {
    }
}