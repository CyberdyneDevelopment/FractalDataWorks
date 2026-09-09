using Fdw.Configuration;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// The role-mapping domain configuration: names which implementation is configured and holds its settings.
/// </summary>
public interface IRoleMappingConfiguration
    : IPlatformServiceConfiguration<IRoleMappingImplementationConfiguration>
{
}
