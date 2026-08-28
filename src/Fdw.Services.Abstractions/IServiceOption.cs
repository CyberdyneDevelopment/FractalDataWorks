using Fdw.Abstractions;

namespace Fdw.Services.Abstractions;

/// <summary>
/// Marks a service interface as the <c>ServiceInterface</c> of a <c>[ServiceTypeCollection]</c> —
/// i.e. a service-type-option service that is resolved through its
/// <see cref="Fdw.ServiceTypes.IPlatformServiceProvider{TService, TConfiguration}"/>. Consumers that need
/// this kind of service MUST inject its <c>IPlatformServiceProvider&lt;TService, TConfiguration&gt;</c> and
/// resolve the concrete instance by name — never inject the service interface (or its implementation)
/// directly.
/// </summary>
public interface IServiceOption : IGenericService
{
    /// <summary>
    /// Gets the configured name this instance was resolved by.
    /// </summary>
    string Name { get; }
}
