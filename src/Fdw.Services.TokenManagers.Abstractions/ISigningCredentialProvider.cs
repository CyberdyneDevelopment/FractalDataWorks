using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Microsoft.IdentityModel.Tokens;

namespace Fdw.Services.TokenManagers.Abstractions;

/// <summary>
/// Supplies the key this platform currently signs with.
/// </summary>
/// <remarks>
/// A seam rather than a configured key, because signing material rotates and lives somewhere a
/// configuration file should not — a secret manager, a key vault, a certificate store. An
/// implementation returning a key it read from disk beside the application is a finding, not a
/// deployment choice.
/// </remarks>
public interface ISigningCredentialProvider
{
    /// <summary>Returns the credentials to sign with now.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IGenericResult<SigningCredentials>> Current(CancellationToken cancellationToken = default);
}
