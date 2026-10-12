using Fdw.Configuration;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>
/// The ResponseBuffering domain configuration: names which implementation is configured and holds its settings.
/// </summary>
/// <remarks>
/// ResponseBuffering is its own domain rather than one option of a host, because a host runs it alongside
/// the other host features rather than instead of them -- a domain names one implementation, and
/// "the host implementation" was never singular.
/// </remarks>
public interface IResponseBufferingConfiguration
    : IDomainConfiguration
{
}
