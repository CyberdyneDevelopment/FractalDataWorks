using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Configuration;

namespace Fdw.Services.Connections;

/// <summary>Supplies the ConnectionAssignment implementation's own configuration to the domain that registers it.</summary>
public interface IConnectionAssignmentImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IConnectionAssignmentImplementationConfiguration>
{
}
