using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Fdw.Data;
using Fdw.Results;
using Fdw.Security.Encryption;
using Fdw.Services.Authentication.Abstractions.Execution;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Flow;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Execution;

/// <summary>
/// Persists single-use sign-in codes in <c>auth.AuthorizationCode</c> (AuthDb), so a code issued by
/// one instance's callback can be redeemed on any instance.
/// </summary>
/// <remarks>
/// The same shape as <see cref="DatabaseExecutionStore"/>. The code is 256 random bits and only its
/// unkeyed SHA-256 is stored. The token is encrypted under the issuing flow's own key. <c>Redeem</c> is
/// one guarded <c>UPDATE ... SET RedeemedAt WHERE CodeHash = @hash AND RedeemedAt IS NULL AND
/// ExpiresAt &gt; now</c>: one row affected means this call won, and zero covers missing, expired and
/// already-redeemed identically.
/// </remarks>
public sealed class DatabaseAuthorizationCodeStore : IAuthorizationCodeStore
{
    private const string PathName = "auth";
    private const string Container = "AuthorizationCode";

    private readonly IConfigurationGatewayProvider _gateways;
    private readonly IAuthenticationContextAccessor _authContextAccessor;
    private readonly IEncryptionCommandExecutor _encryption;
    private readonly IAuthenticationFlowProvider _flows;
    private readonly ILogger<DatabaseAuthorizationCodeStore> _logger;

    /// <summary>Initializes a new instance of the <see cref="DatabaseAuthorizationCodeStore"/> class.</summary>
    /// <param name="gateways">Holds the gateway for the host-declared AuthDb connection, the only path to the authentication state (FDW-1084).</param>
    /// <param name="authContextAccessor">
    /// Lets each call run as the system principal: the browser redeeming a code is not signed in yet.
    /// </param>
    /// <param name="encryption">Encrypts and decrypts the stored token.</param>
    /// <param name="flows">Names, per flow, the secret manager and key that encrypt its tokens.</param>
    /// <param name="logger">The logger.</param>
    public DatabaseAuthorizationCodeStore(
        IConfigurationGatewayProvider gateways,
        IAuthenticationContextAccessor authContextAccessor,
        IEncryptionCommandExecutor encryption,
        IAuthenticationFlowProvider flows,
        ILogger<DatabaseAuthorizationCodeStore>? logger = null)
    {
        _gateways = gateways ?? throw new ArgumentNullException(nameof(gateways));
        _authContextAccessor = authContextAccessor ?? throw new ArgumentNullException(nameof(authContextAccessor));
        _encryption = encryption ?? throw new ArgumentNullException(nameof(encryption));
        _flows = flows ?? throw new ArgumentNullException(nameof(flows));
        _logger = logger ?? NullLogger<DatabaseAuthorizationCodeStore>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<string>> Issue(
        string flowName, IssuedToken token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(flowName) || token is null)
            return GenericResult<string>.Failure(DatabaseAuthorizationCodeStoreLog.IssueArgumentsMissing(_logger));

        using var scope = new SystemAuthenticationContextScope(_authContextAccessor);

        var flow = await _flows.Get(flowName, cancellationToken).ConfigureAwait(false);
        if (flow.IsFailure)
            return flow.ToNewResult<string>();

        if (flow.Value!.SecretManagerName is not { } secretManagerName || flow.Value.SecretKeyName is not { } secretKeyName)
            return GenericResult<string>.Failure(DatabaseAuthorizationCodeStoreLog.NoExecutionKey(_logger, flowName));

        // Why from the flow (FDW-898): how long a code stays redeemable is the deployment's per-flow
        // decision (auth.AuthenticationFlowConfiguration.CodeLifetime), not a compiled-in constant.
        if (flow.Value.CodeLifetime is not { } codeLifetime)
            return GenericResult<string>.Failure(DatabaseAuthorizationCodeStoreLog.NoCodeLifetime(_logger, flowName));

        var encrypted = await _encryption.Execute(
            new EncryptCommand
            {
                SecretManagerName = secretManagerName,
                SecretKeyName = secretKeyName,
                Plaintext = JsonSerializer.SerializeToUtf8Bytes(token),
            },
            cancellationToken).ConfigureAwait(false);
        if (encrypted.IsFailure)
            return encrypted.ToNewResult<string>();

        var code = Base64Url(RandomNumberGenerator.GetBytes(32));

        var call = Insert.Into<AuthorizationCodeEntity>(Container)
            .DataStore(AuthenticationStepTypes.StateConnection)
            .Path(PathName)
            .Value(new AuthorizationCodeEntity
            {
                CodeHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(code)),
                FlowName = flowName,
                EncryptedToken = encrypted.Value!,
                ExpiresAt = DateTimeOffset.UtcNow.Add(codeLifetime),
            });

        var result = await _gateways.Execute<int>(AuthenticationStepTypes.StateConnection, call, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            DatabaseAuthorizationCodeStoreLog.IssueWriteFailed(_logger, flowName);
            return result.ToNewResult<string>();
        }

        DatabaseAuthorizationCodeStoreLog.Issued(_logger, flowName);
        return GenericResult<string>.Success(code);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<RedeemedCode>> Redeem(
        string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(code))
            return GenericResult<RedeemedCode>.Failure(DatabaseAuthorizationCodeStoreLog.CodeMissing(_logger));

        using var scope = new SystemAuthenticationContextScope(_authContextAccessor);

        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(code));
        var now = DateTimeOffset.UtcNow;

        var redeemCall = Update.In<AuthorizationCodeRedeemUpdate>(Container)
            .DataStore(AuthenticationStepTypes.StateConnection)
            .Path(PathName)
            .Where(nameof(AuthorizationCodeEntity.CodeHash), hash)
            .Where(nameof(AuthorizationCodeEntity.RedeemedAt), new IsNullOperator(), null)
            .Where(nameof(AuthorizationCodeEntity.ExpiresAt), new GreaterThanOperator(), now)
            .Value(new AuthorizationCodeRedeemUpdate { RedeemedAt = now });

        var redeemed = await _gateways.Execute<int>(AuthenticationStepTypes.StateConnection, redeemCall, cancellationToken).ConfigureAwait(false);
        if (redeemed.IsFailure || redeemed.Value != 1)
        {
            // Missing, expired, and already-redeemed land here identically: telling them apart would
            // tell a caller the code was real.
            return GenericResult<RedeemedCode>.Failure(DatabaseAuthorizationCodeStoreLog.NotRedeemable(_logger));
        }

        // Safe now, not a race: the guarded UPDATE reporting one row means the row is ours alone.
        var readCall = Query.From<AuthorizationCodeEntity>(AuthenticationStepTypes.StateConnection, PathName, Container)
            .Where(r => r.CodeHash).Equal(hash)
            .Build();

        var read = await _gateways.Execute<IEnumerable<AuthorizationCodeEntity>>(AuthenticationStepTypes.StateConnection, readCall, cancellationToken)
            .ConfigureAwait(false);
        if (read.IsFailure)
        {
            DatabaseAuthorizationCodeStoreLog.RedeemReadBackFailed(_logger);
            return read.ToNewResult<RedeemedCode>();
        }

        var entity = read.Value!.FirstOrDefault();
        if (entity is null)
            return GenericResult<RedeemedCode>.Failure(DatabaseAuthorizationCodeStoreLog.RedeemReadBackFailed(_logger));

        var flow = await _flows.Get(entity.FlowName, cancellationToken).ConfigureAwait(false);
        if (flow.IsFailure)
            return flow.ToNewResult<RedeemedCode>();

        if (flow.Value!.SecretManagerName is not { } secretManagerName || flow.Value.SecretKeyName is not { } secretKeyName)
            return GenericResult<RedeemedCode>.Failure(DatabaseAuthorizationCodeStoreLog.NoExecutionKey(_logger, entity.FlowName));

        var plaintext = await _encryption.Execute(
            new DecryptCommand
            {
                SecretManagerName = secretManagerName,
                SecretKeyName = secretKeyName,
                Ciphertext = entity.EncryptedToken,
            },
            cancellationToken).ConfigureAwait(false);
        if (plaintext.IsFailure)
            return plaintext.ToNewResult<RedeemedCode>();

        var token = JsonSerializer.Deserialize<IssuedToken>(plaintext.Value!);
        if (token is null)
            return GenericResult<RedeemedCode>.Failure(DatabaseAuthorizationCodeStoreLog.TokenUnreadable(_logger, entity.FlowName));

        DatabaseAuthorizationCodeStoreLog.Redeemed(_logger, entity.FlowName);
        return GenericResult<RedeemedCode>.Success(new RedeemedCode { FlowName = entity.FlowName, Token = token });
    }

    private static string Base64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
