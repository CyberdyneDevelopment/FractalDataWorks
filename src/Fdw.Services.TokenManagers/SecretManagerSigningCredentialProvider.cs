using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Commands;
using Fdw.Services.TokenManagers.Abstractions;
using Fdw.Services.TokenManagers.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace Fdw.Services.TokenManagers;

/// <summary>
/// Supplies the signing key from the secret manager, by name.
/// </summary>
/// <remarks>
/// The key never appears in configuration, on disk beside the application, or in a container image.
/// It is fetched by name from whatever the host configured as its secret manager, cached for as long
/// as it is expected to live, and dropped when that expires so a rotation takes effect without a
/// restart.
/// </remarks>
public sealed class SecretManagerSigningCredentialProvider : ISigningCredentialProvider, IDisposable
{
    private readonly ISecretManagerProvider _secrets;
    private readonly string _secretManagerName;
    private readonly string _keyName;
    private readonly TimeSpan _cacheLifetime;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ILogger<SecretManagerSigningCredentialProvider> _logger;

    private SigningCredentials? _cached;
    private DateTimeOffset _cachedAt;

    /// <summary>Initializes a new instance of the <see cref="SecretManagerSigningCredentialProvider"/> class.</summary>
    /// <param name="secrets">Resolves the configured secret manager.</param>
    /// <param name="secretManagerName">Which secret manager holds the key.</param>
    /// <param name="keyName">The key's name within it.</param>
    /// <param name="cacheLifetime">How long a fetched key is reused.</param>
    /// <param name="logger">The logger.</param>
    public SecretManagerSigningCredentialProvider(
        ISecretManagerProvider secrets,
        string secretManagerName,
        string keyName,
        TimeSpan cacheLifetime,
        ILogger<SecretManagerSigningCredentialProvider>? logger = null)
    {
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _secretManagerName = secretManagerName ?? throw new ArgumentNullException(nameof(secretManagerName));
        _keyName = keyName ?? throw new ArgumentNullException(nameof(keyName));
        _cacheLifetime = cacheLifetime;
        _logger = logger ?? NullLogger<SecretManagerSigningCredentialProvider>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<SigningCredentials>> Current(CancellationToken cancellationToken = default)
    {
        if (_cached is not null && _cachedAt.Add(_cacheLifetime) > DateTimeOffset.UtcNow)
        {
            IssuerLog.SigningKeyReused(_logger, _keyName);
            return GenericResult<SigningCredentials>.Success(_cached);
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cached is not null && _cachedAt.Add(_cacheLifetime) > DateTimeOffset.UtcNow)
                return GenericResult<SigningCredentials>.Success(_cached);

            var manager = await _secrets.Get(_secretManagerName, cancellationToken).ConfigureAwait(false);
            if (manager.IsFailure)
                return manager.ToNewResult<SigningCredentials>();

            var secret = await manager.Value!
                .Execute<SecretValue>(new GetSecretManagerCommand(container: null, secretKey: _keyName), cancellationToken)
                .ConfigureAwait(false);

            if (secret.IsFailure)
                return secret.ToNewResult<SigningCredentials>();

            RSA rsa;
            try
            {
                rsa = secret.Value!.AccessStringValue(pem =>
                {
                    var created = RSA.Create();
                    created.ImportFromPem(pem);
                    return created;
                });
            }
            catch (ArgumentException ex)
            {
                // Carried rather than swallowed. ImportFromPem reports that no supported format was
                // found and does not echo the material, so recording it is safe here — which is not
                // true of every parser, and worth checking before doing the same elsewhere.
                return GenericResult<SigningCredentials>.Failure(
                    IssuerLog.SigningKeyUnreadable(_logger, ex, _keyName));
            }

            _cached = new SigningCredentials(
                new RsaSecurityKey(rsa) { KeyId = _keyName },
                SecurityAlgorithms.RsaSha256);
            _cachedAt = DateTimeOffset.UtcNow;

            IssuerLog.SigningKeyLoaded(_logger, _keyName);
            return GenericResult<SigningCredentials>.Success(_cached);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();
}
