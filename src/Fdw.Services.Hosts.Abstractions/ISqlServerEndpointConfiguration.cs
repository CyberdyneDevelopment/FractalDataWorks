using Fdw.Configuration;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>
/// The SQL Server endpoint domain configuration: names which implementation is configured and holds its settings.
/// </summary>
public interface ISqlServerEndpointConfiguration
    : IDomainConfiguration
{
}
