using Fdw.ServiceTypes;

namespace Fdw.Services.Identity.Abstractions;

/// <summary>
/// Resolves identity services by configuration name or id.
/// </summary>
public interface IIdentityServiceProvider
    : IPlatformServiceProvider<IIdentityService, IIdentityServiceImplementationConfiguration>
{
}
