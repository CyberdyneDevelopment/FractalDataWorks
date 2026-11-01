using Fdw.Configuration;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>
/// One implementation of the SQL Server endpoint domain: how a host listens for SQL Server clients over TDS,
/// and the server it presents itself as to them.
/// </summary>
public interface ISqlServerEndpointImplementationConfiguration : IImplementationConfiguration
{
}
