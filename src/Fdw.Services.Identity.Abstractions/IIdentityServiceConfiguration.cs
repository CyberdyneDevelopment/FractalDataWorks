using Fdw.Configuration;

namespace Fdw.Services.Identity.Abstractions;

/// <summary>
/// One configured identity service — the domain record, naming which implementation it is and holding
/// that implementation's own configuration.
/// </summary>
public interface IIdentityServiceConfiguration
    : IPlatformServiceConfiguration<IIdentityServiceImplementationConfiguration>
{
}
