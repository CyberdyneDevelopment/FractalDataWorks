using Fdw.Configuration;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>
/// The contract every ResponseBuffering implementation's configuration satisfies -- which responses a host buffers before writing.
/// </summary>
public interface IResponseBufferingImplementationConfiguration : IImplementationConfiguration
{
}
