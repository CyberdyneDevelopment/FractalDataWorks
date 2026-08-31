using Fdw.Services.Abstractions;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// Reads the LocalKey rows of <c>auth.AuthenticationService</c>.
/// </summary>
/// <remarks>
/// The option's own provider interface. The domain provider registers one of these per kind and
/// dispatches to it by the kind the domain row names; a consumer that needs LocalKey rows
/// specifically asks for this rather than for the domain provider.
/// </remarks>
public interface ILocalKeyAuthenticationConfigurationProvider
    : IImplementationConfigurationProvider<ILocalKeyAuthenticationConfiguration>
{
}
