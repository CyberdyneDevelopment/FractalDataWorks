using Fdw.Abstractions;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Workspaces;

/// <summary>
/// Factory interface for creating the place domain's service instances (FDW-2538).
/// </summary>
public interface IPlaceServiceFactory : IServiceFactory<IGenericService, IServiceConfiguration>
{
}
