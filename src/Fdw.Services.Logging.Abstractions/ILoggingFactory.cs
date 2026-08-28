using Fdw.Abstractions;
using Fdw.Configuration;

namespace Fdw.Services.Logging.Abstractions;

/// <summary>Non-generic marker so factories can be held without naming their type arguments.</summary>
public interface ILoggingFactory
{
}

/// <summary>Builds a logging service from its implementation configuration.</summary>
/// <typeparam name="TService">The logging service this factory creates.</typeparam>
/// <typeparam name="TConfiguration">The implementation configuration it builds from.</typeparam>
public interface ILoggingFactory<TService, TConfiguration>
    : ILoggingFactory, IServiceFactory<TService, TConfiguration>
    where TService : ILoggingService
    where TConfiguration : IGenericConfiguration
{
}
