using System;
using System.Security.Cryptography;
using Fdw.Results;
using Fdw.Services.SecretProtection.Abstractions;
using Fdw.Services.SecretProtection.DataProtection.Logging;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.SecretProtection.DataProtection;

/// <summary>
/// <see cref="ISecretProtector"/> over ASP.NET Core's Data Protection stack.
/// </summary>
/// <remarks>
/// Converts <see cref="CryptographicException"/> at the Protect/Unprotect boundary into a failure
/// result rather than letting it escape -- the natural shape for a call whose failure mode is routine
/// (a stale or foreign key ring) rather than exceptional.
/// </remarks>
internal sealed class DataProtectionSecretProtector : ISecretProtector
{
    private const string Purpose = "Fdw.Services.Authentication.ExecutionContext";

    private readonly IDataProtector _protector;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="DataProtectionSecretProtector"/> class.</summary>
    /// <param name="provider">The Data Protection provider this deployment configured.</param>
    /// <param name="logger">The logger.</param>
    public DataProtectionSecretProtector(IDataProtectionProvider provider, ILogger<DataProtectionSecretProtector>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _protector = provider.CreateProtector(Purpose);
        _logger = logger ?? NullLogger<DataProtectionSecretProtector>.Instance;
    }

    /// <inheritdoc/>
    public IGenericResult<byte[]> Protect(byte[] plaintext)
    {
        try
        {
            return GenericResult<byte[]>.Success(_protector.Protect(plaintext));
        }
        catch (CryptographicException ex)
        {
            return GenericResult<byte[]>.Failure(DataProtectionSecretProtectorLog.ProtectFailed(_logger, ex));
        }
    }

    /// <inheritdoc/>
    public IGenericResult<byte[]> Unprotect(byte[] ciphertext)
    {
        try
        {
            return GenericResult<byte[]>.Success(_protector.Unprotect(ciphertext));
        }
        catch (CryptographicException ex)
        {
            return GenericResult<byte[]>.Failure(DataProtectionSecretProtectorLog.UnprotectFailed(_logger, ex));
        }
    }
}
