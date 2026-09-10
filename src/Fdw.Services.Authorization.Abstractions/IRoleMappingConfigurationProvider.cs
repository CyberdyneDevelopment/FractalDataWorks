using Fdw.Services.Abstractions;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// Supplies role-mapping configuration. Registered and resolved as this type — never as the base.
/// </summary>
/// <remarks>
/// Returns the implementation the domain row names, so a caller asking for the system mapping gets
/// the roles rather than a domain record it would have to unwrap.
/// </remarks>
public interface IRoleMappingConfigurationProvider
    : IDomainConfigurationProvider<IRoleMappingImplementationConfiguration>
{
}
