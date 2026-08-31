using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// Reads the authentication services a host trusts, from <c>auth.AuthenticationService</c>.
/// </summary>
/// <remarks>
/// Named rather than a bare closed generic: a constructor asking for this states which rows it reads.
/// Two providers over different tables that share a shape are interchangeable at a call site when
/// both are spelled <c>IServiceConfigurationProvider&lt;T&gt;</c>, and nothing catches the swap.
/// </remarks>
public interface IAuthenticationServiceConfigurationProvider
    : IDomainConfigurationProvider<IAuthenticationServiceImplementationConfiguration>
{
    /// <summary>Reads the declared services, without dispatching to their implementations.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <remarks>
    /// Scheme routing needs the name, the kind, the authority and whether the entry is enabled — all
    /// on the domain row. What each kind uses to check a signature is read later, by the provider for
    /// that kind, when a token actually arrives.
    /// </remarks>
    Task<IGenericResult<IReadOnlyList<IAuthenticationServiceConfiguration>>> GetHeaders(
        CancellationToken cancellationToken = default);
}
