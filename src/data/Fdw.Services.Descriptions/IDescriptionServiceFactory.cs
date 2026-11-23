using Fdw.Abstractions;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Descriptions;

/// <summary>Factory interface for creating descriptions domain service instances.</summary>
public interface IDescriptionServiceFactory : IServiceFactory<IGenericService, IServiceConfiguration>
{
}
