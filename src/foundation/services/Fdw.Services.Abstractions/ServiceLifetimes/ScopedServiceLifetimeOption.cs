using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Abstractions;

/// <summary>
/// Scoped lifetime - new instance created once per scope (typically per request).
/// </summary>
/// <remarks>
/// Use for services that need to maintain state during a single operation or request.
/// In web applications, this typically means one instance per HTTP request.
/// Balances performance and isolation - shared within scope, isolated between scopes.
/// </remarks>
public sealed class ScopedServiceLifetimeOption : ServiceLifetimeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScopedServiceLifetimeOption"/> class.
    /// </summary>
    public ScopedServiceLifetimeOption() : base(2, "Scoped", "New instance per scope/request", ServiceLifetime.Scoped)
    {
    }
}