using Fdw.Configuration;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>
/// The contract every OutboundIdentity implementation's configuration satisfies -- how a host obtains the token it
/// presents to the services it calls.
/// </summary>
/// <remarks>
/// Deliberately empty, as every host implementation contract is: the settings live on the implementation's
/// own record, so adding a setting never widens a shared interface.
/// </remarks>
public interface IOutboundIdentityImplementationConfiguration : IImplementationConfiguration
{
}
