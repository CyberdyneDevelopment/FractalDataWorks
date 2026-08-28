using Fdw.Abstractions;
using Fdw.Configuration;

namespace Fdw.Services.Hosting.Abstractions;

/// <summary>Non-generic marker so factories can be held without naming their type arguments.</summary>
public interface IHostingFactory
{
}

/// <summary>Builds a hosting service from its implementation configuration.</summary>
/// <typeparam name="TService">The hosting service this factory creates.</typeparam>
/// <typeparam name="TConfiguration">The implementation configuration it builds from.</typeparam>
public interface IHostingFactory<TService, TConfiguration>
    : IHostingFactory, IServiceFactory<TService, TConfiguration>
    where TService : IHostingService
    where TConfiguration : IGenericConfiguration
{
}
