using Fdw.Abstractions;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Settings;

/// <summary>
/// Factory interface for creating settings domain service instances.
/// </summary>
public interface ISettingsServiceFactory : IServiceFactory<IGenericService, IServiceConfiguration>
{
}
