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
    // Why: this interface's whole contract is "resolved through its provider BY NAME". Anything
    // resolved by name must be able to state which name it is, or a caller holding one cannot verify
    // it got what it asked for — which is exactly what a connection needs before it reads a secret
    // out of a store the configuration named.
    string Name { get; }
}
