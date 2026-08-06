using RootNamespace.ServiceName.Abstractions;

namespace RootNamespace.ServiceName.ImplName;

/// <summary>
/// Factory interface for creating ImplName ServiceName service instances.
/// </summary>
/// <remarks>
/// <para>
/// This interface is registered with DI in Phase 1 and resolved in Phase 2
/// for registration with the provider.
/// </para>
/// </remarks>
public interface IImplNameServiceNameFactory : IServiceNameFactory
{
    // Inherits Create(IServiceNameConfiguration) from base interface
}
