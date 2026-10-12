using Fdw.Configuration;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>
/// The ResponseStreaming domain configuration: names which implementation is configured and holds its settings.
/// </summary>
/// <remarks>
/// ResponseStreaming is its own domain rather than one option of a host, because a host runs it alongside
/// the other host features rather than instead of them -- a domain names one implementation, and
/// "the host implementation" was never singular. It is the sibling of ResponseBuffering: one decides
/// whether a response is held before it is written, the other the limits under which a response is
/// written progressively.
/// </remarks>
public interface IResponseStreamingConfiguration
    : IDomainConfiguration
{
}
