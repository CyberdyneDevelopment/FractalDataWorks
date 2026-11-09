using Fdw.Configuration;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Identity.Abstractions;

namespace Fdw.Services.Identity;

/// <summary>
/// Resolves configured identity services and routes each to the implementation provider that owns it,
/// and reads their list from the identities domain table alone.
/// </summary>
public interface IIdentityServiceConfigurationProvider
    : IDomainConfigurationProvider<IIdentityServiceImplementationConfiguration>,
      IDomainRecordProvider<DomainConfiguration>
{
}
