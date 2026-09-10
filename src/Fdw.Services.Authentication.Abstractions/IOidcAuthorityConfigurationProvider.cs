using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// Reads which OIDC provider a deployment trusts, and on what terms.
/// </summary>
/// <remarks>
/// Read asynchronously because the row lives in ConfigurationDb. The OidcRedirect step's
/// <c>Execute</c> reads through this rather than taking the configuration itself, so the option can
/// be built with a parameterless constructor and populate what it needs during initialization.
/// </remarks>
public interface IOidcAuthorityConfigurationProvider
{

}
