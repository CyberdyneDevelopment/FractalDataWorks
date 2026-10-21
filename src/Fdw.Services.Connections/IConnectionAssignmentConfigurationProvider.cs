using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Configuration;

namespace Fdw.Services.Connections;

/// <summary>Supplies the ConnectionAssignment configuration (FDW-1581).</summary>
public interface IConnectionAssignmentConfigurationProvider
    : IDomainConfigurationProvider<IConnectionAssignmentImplementationConfiguration>
{
}
