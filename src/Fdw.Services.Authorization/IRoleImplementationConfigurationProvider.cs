using Fdw.Services.Abstractions;
using Fdw.Services.Authorization.Configuration;

namespace Fdw.Services.Authorization;

/// <summary>Supplies the Role implementation's own configuration to the domain that registers it.</summary>
public interface IRoleImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IRoleImplementationConfiguration>
{
}
