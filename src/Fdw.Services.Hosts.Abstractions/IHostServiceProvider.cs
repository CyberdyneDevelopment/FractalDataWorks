using Fdw.ServiceTypes;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>
/// Resolves hosting services by configuration name or id.
/// </summary>
public interface IHostServiceProvider
    : IPlatformServiceProvider<IHostService, IHostImplementationConfiguration>
{
}
