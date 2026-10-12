using Fdw.Services.Abstractions;
using Fdw.Services.Identity.Abstractions;

namespace Fdw.Services.Identity.JwtAssertion;

/// <summary>Supplies this implementation's own configuration to the domain that registers it.</summary>
public interface IJwtAssertionConfigurationProvider
    : IImplementationConfigurationProvider<IIdentityServiceImplementationConfiguration>
{
}
