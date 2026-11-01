using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Configuration;

namespace Fdw.Services.Multitenancy;

/// <summary>Supplies the configured organizations: a member whole, or a page of them as domain records.</summary>
public interface IOrganizationConfigurationProvider
    : IDomainConfigurationProvider<IOrganizationImplementationConfiguration>,
      IDomainRecordProvider<OrganizationDomainConfiguration>
{
}
