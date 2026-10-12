using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace Fdw.Services.TokenManagers;

/// <summary>
/// The <see cref="ISigningCredentialProvider"/> a host resolves from its own configuration.
/// </summary>
/// <remarks>
/// The validating half of what <see cref="ConfiguredTokenIssuer"/> mints with. Both go through one
/// resolver, so the scheme that checks a signature and the issuer that made it cannot end up on
/// different keys.
/// </remarks>
internal sealed class ConfiguredSigningCredentialProvider : ISigningCredentialProvider
{
    private readonly JwtIssuanceResolver _resolver;

    /// <summary>Initializes a new instance of the <see cref="ConfiguredSigningCredentialProvider"/> class.</summary>
    /// <param name="resolver">Resolves the configured key.</param>
    public ConfiguredSigningCredentialProvider(JwtIssuanceResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<SigningCredentials>> Current(
        CancellationToken cancellationToken = default)
    {
        var credentials = await _resolver.Credentials(cancellationToken).ConfigureAwait(false);

        return credentials.IsFailure || credentials.Value is not { } resolved
            ? credentials.ToNewResult<SigningCredentials>()
            : await resolved.Current(cancellationToken).ConfigureAwait(false);
    }
}
