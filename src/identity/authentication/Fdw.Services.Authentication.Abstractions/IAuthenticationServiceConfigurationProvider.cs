using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// Reads the authentication services a host trusts, from <c>auth.Authentication</c>.
/// </summary>
/// <remarks>
/// Named rather than a bare closed generic: a constructor asking for this states which rows it reads.
/// Two providers over different tables that share a shape are interchangeable at a call site when
/// both are spelled <c>IServiceConfigurationProvider&lt;T&gt;</c>, and nothing catches the swap.
/// </remarks>
public interface IAuthenticationServiceConfigurationProvider
    : IDomainConfigurationProvider<IAuthenticationServiceImplementationConfiguration>
{

}
