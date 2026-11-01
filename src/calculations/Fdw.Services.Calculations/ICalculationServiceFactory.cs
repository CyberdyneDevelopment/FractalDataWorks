using Fdw.Abstractions;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Calculations;

/// <summary>
/// Factory interface for creating calculation domain service instances.
/// </summary>
public interface ICalculationServiceFactory : IServiceFactory<IGenericService, IServiceConfiguration>
{
}
