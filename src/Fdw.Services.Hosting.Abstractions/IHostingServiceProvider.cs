using Fdw.ServiceTypes;

namespace Fdw.Services.Hosting.Abstractions;

/// <summary>
/// Resolves hosting services by configuration name or id.
/// </summary>
public interface IHostingServiceProvider
    : IPlatformServiceProvider<IHostingService, IHostingImplementationConfiguration>
{
}
