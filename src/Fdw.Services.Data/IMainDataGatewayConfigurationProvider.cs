using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data;

/// <summary>Supplies this implementation's own configuration to the domain that registers it.</summary>
public interface IMainDataGatewayConfigurationProvider
    : IImplementationConfigurationProvider<IDataGatewayImplementationConfiguration>
{
}
