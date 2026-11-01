using Fdw.Configuration;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>
/// The contract every ResponseStreaming implementation's configuration satisfies -- the limits under which a host streams responses.
/// </summary>
/// <remarks>
/// Deliberately empty, as every host implementation contract is: the settings live on the implementation's
/// own record, so adding a setting never widens a shared interface.
/// </remarks>
public interface IResponseStreamingImplementationConfiguration : IImplementationConfiguration
{
}
