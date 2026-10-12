using Fdw.Configuration;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>
/// The OutboundIdentity domain configuration: names which implementation is configured and holds its settings.
/// </summary>
/// <remarks>
/// OutboundIdentity is the identity a host presents when it calls another service -- the token its outbound
/// HTTP clients attach. It is its own domain for the same reason the other host features are: a host runs it
/// alongside them rather than instead of them, so it is never "the" host implementation.
/// </remarks>
public interface IOutboundIdentityConfiguration
    : IDomainConfiguration
{
}
