using Fdw.Configuration;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// An authentication service a host trusts to have issued a token.
/// </summary>
/// <remarks>
/// The typed domain contract. A constructor asking for this states which domain it reads; one asking
/// for the closed generic states only a shape, and two domains that share a shape become
/// interchangeable at the call site.
/// </remarks>
public interface IAuthenticationServiceConfiguration
    : IPlatformServiceConfiguration<IAuthenticationServiceImplementationConfiguration>
{
}
