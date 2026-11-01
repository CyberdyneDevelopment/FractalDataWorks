using Fdw.Services.Abstractions;
using Fdw.Services.Credentials.Sql.Configuration;

namespace Fdw.Services.Credentials.Sql;

/// <summary>
/// Supplies credential-store configuration. Registered and resolved as this type — never as the base.
/// </summary>
public interface ICredentialStoreConfigurationProvider
    : IDomainConfigurationProvider<ICredentialStoreImplementationConfiguration>
{
}
