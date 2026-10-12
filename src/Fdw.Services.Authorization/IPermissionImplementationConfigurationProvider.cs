using Fdw.Services.Abstractions;
using Fdw.Services.Authorization.Configuration;

namespace Fdw.Services.Authorization;

/// <summary>Supplies the Permission implementation's own configuration to the domain that registers it.</summary>
public interface IPermissionImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IPermissionImplementationConfiguration>
{
}
