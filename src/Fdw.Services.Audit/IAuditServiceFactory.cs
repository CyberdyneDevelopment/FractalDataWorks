using Fdw.Abstractions;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Audit;

/// <summary>
/// Factory interface for creating audit service instances.
/// </summary>
public interface IAuditServiceFactory : IServiceFactory<IGenericService, IServiceConfiguration>
{
}
