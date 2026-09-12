using Fdw.ServiceTypes;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>
/// Resolves hosting services by configuration name or id.
/// </summary>
public interface IHostServiceProvider
    : IDomainServiceProvider<IHostService, IHostImplementationConfiguration>
{
}
