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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Execution;

/// <summary>
/// Persists suspended flows in <c>auth.AuthenticationExecution</c>/<c>auth.ConsumedResumeToken</c>
/// (AuthDb), so a resume works regardless of which instance handles the caller's
/// return.
/// </summary>
/// <remarks>
/// <c>TryConsume</c> is one atomic <c>UPDATE ... WHERE ResumeTokenHash = @hash AND ConsumedAt IS NULL
/// AND ExpiresAt &gt; now</c> built from the narrow <see cref="AuthenticationExecutionConsumeUpdate"/>
/// shape — rows-affected is the only signal: 1 means this call won the race, 0 covers missing,
/// expired, and already-consumed identically, exactly as the table's own <c>MS_Description</c>
/// requires. The resume token itself is hashed with unkeyed SHA-256 before it ever reaches a query —
/// it already carries 256 bits of entropy, so a keyed HMAC buys nothing a plain hash doesn't.
/// </remarks>
public sealed class DatabaseExecutionStore : IAuthenticationExecutionStore
{
    private const string PathName = "auth";
    private const string ExecutionContainer = "AuthenticationExecution";
    private const string ConsumedTokenContainer = "ConsumedResumeToken";


    private readonly IConfigurationGatewayProvider _gateways;
    private readonly IAuthenticationContextAccessor _authContextAccessor;
    private readonly IEncryptionCommandExecutor _encryption;
    private readonly IAuthenticationFlowProvider _flows;
    private readonly ILogger<DatabaseExecutionStore> _logger;

    /// <summary>Initializes a new instance of the <see cref="DatabaseExecutionStore"/> class.</summary>
    /// <param name="gateways">Holds the gateway for the host-declared AuthDb connection, the only path to the authentication state (FDW-1084).</param>
    /// <param name="authContextAccessor">
    /// Lets each call run as the system principal: a suspended flow belongs to no signed-in user yet,
    /// and AuthDb's container rows are invisible to an anonymous caller.
    /// </param>
    /// <param name="encryption">Encrypts and decrypts ContextData.</param>
    /// <param name="flows">
    /// Names, per flow, the secret manager and key that encrypt its suspended executions. There is no
    /// process-wide key: each record is encrypted and decrypted under the key its own flow names.
    /// </param>
    /// <param name="logger">The logger.</param>
    public DatabaseExecutionStore(
        IConfigurationGatewayProvider gateways,
        IAuthenticationContextAccessor authContextAccessor,
        IEncryptionCommandExecutor encryption,
        IAuthenticationFlowProvider flows,
        ILogger<DatabaseExecutionStore>? logger = null)
    {
        _gateways = gateways ?? throw new ArgumentNullException(nameof(gateways));
        _authContextAccessor = authContextAccessor ?? throw new ArgumentNullException(nameof(authContextAccessor));
        _encryption = encryption ?? throw new ArgumentNullException(nameof(encryption));
        _flows = flows ?? throw new ArgumentNullException(nameof(flows));
        _logger = logger ?? NullLogger<DatabaseExecutionStore>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<string>> Suspend(
        ExecutionRecord record, CancellationToken cancellationToken = default)
    {
        if (record is null)
            return GenericResult<string>.Failure(DatabaseExecutionStoreLog.RecordMissing(_logger));

        using var scope = new SystemAuthenticationContextScope(_authContextAccessor);

        var flow = await _flows.Get(record.FlowName, cancellationToken).ConfigureAwait(false);
        if (flow.IsFailure)
            return flow.ToNewResult<string>();

        if (flow.Value!.SecretManagerName is not { } secretManagerName || flow.Value.SecretKeyName is not { } secretKeyName)
            return GenericResult<string>.Failure(DatabaseExecutionStoreLog.NoExecutionKey(_logger, record.FlowName));

        var token = Base64Url(RandomNumberGenerator.GetBytes(32));
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));

        var contextData = await _encryption.Execute(
            new EncryptCommand
            {
                SecretManagerName = secretManagerName,
                SecretKeyName = secretKeyName,
                Plaintext = JsonSerializer.SerializeToUtf8Bytes(AuthenticationContextData.FromContext(record.Context)),
            },
            cancellationToken).ConfigureAwait(false);
        if (contextData.IsFailure)
            return contextData.ToNewResult<string>();

        var call = Insert.Into<AuthenticationExecutionEntity>(ExecutionContainer)
            .DataStore(AuthenticationStepTypes.StateConnection)
            .Path(PathName)
            .Value(new AuthenticationExecutionEntity
            {
                Id = record.Id,
                FlowName = record.FlowName,
                ResumeTokenHash = hash,
                ContextData = contextData.Value!,
                CurrentStepIndex = record.CurrentStepIndex,
                ExpiresAt = record.ExpiresAt,
            });

        var result = await _gateways.Execute<int>(AuthenticationStepTypes.StateConnection, call, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            DatabaseExecutionStoreLog.SuspendWriteFailed(_logger, record.Id);
            return result.ToNewResult<string>();
        }

        DatabaseExecutionStoreLog.Suspended(_logger, record.Id, record.FlowName, record.CurrentStepIndex);
        return GenericResult<string>.Success(token);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<ExecutionRecord>> TryConsume(
        string resumeToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(resumeToken))
            return GenericResult<ExecutionRecord>.Failure(DatabaseExecutionStoreLog.TokenMissing(_logger));

        using var scope = new SystemAuthenticationContextScope(_authContextAccessor);

        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(resumeToken));
        var now = DateTimeOffset.UtcNow;

        var consumeCall = Update.In<AuthenticationExecutionConsumeUpdate>(ExecutionContainer)
            .DataStore(AuthenticationStepTypes.StateConnection)
            .Path(PathName)
            .Where(nameof(AuthenticationExecutionEntity.ResumeTokenHash), hash)
            .Where(nameof(AuthenticationExecutionEntity.ConsumedAt), new IsNullOperator(), null)
            .Where(nameof(AuthenticationExecutionEntity.ExpiresAt), new GreaterThanOperator(), now)
            .Value(new AuthenticationExecutionConsumeUpdate { ConsumedAt = now });

        var consumeResult = await _gateways.Execute<int>(AuthenticationStepTypes.StateConnection, consumeCall, cancellationToken).ConfigureAwait(false);
        if (consumeResult.IsFailure || consumeResult.Value != 1)
        {
            // Missing, expired, and already-consumed all land here, identically — see the table's
            // own MS_Description. Distinguishing them would tell a caller a token was real, only stale.
            return GenericResult<ExecutionRecord>.Failure(DatabaseExecutionStoreLog.NotResumable(_logger));
        }

        // Safe now, not a race: the row is exclusively ours the moment the guarded UPDATE above
        // reports 1 row affected, so reading it back is a plain lookup on a row nobody else can touch.
        var readCall = Query.From<AuthenticationExecutionEntity>(AuthenticationStepTypes.StateConnection, PathName, ExecutionContainer)
            .Where(r => r.ResumeTokenHash).Equal(hash)
            .Build();

        var readResult = await _gateways.Execute<IEnumerable<AuthenticationExecutionEntity>>(AuthenticationStepTypes.StateConnection, readCall, cancellationToken)
            .ConfigureAwait(false);
        if (readResult.IsFailure)
        {
            DatabaseExecutionStoreLog.ConsumeReadBackFailed(_logger);
            return readResult.ToNewResult<ExecutionRecord>();
        }

        var entity = readResult.Value!.FirstOrDefault();
        if (entity is null)
            return GenericResult<ExecutionRecord>.Failure(DatabaseExecutionStoreLog.ConsumeReadBackFailed(_logger));

        var insertConsumed = Insert.Into<ConsumedResumeTokenEntity>(ConsumedTokenContainer)
            .DataStore(AuthenticationStepTypes.StateConnection)
            .Path(PathName)
            .Value(new ConsumedResumeTokenEntity { ResumeTokenHash = hash, ExpiresAt = entity.ExpiresAt });

        var consumedTokenResult = await _gateways.Execute<int>(AuthenticationStepTypes.StateConnection, insertConsumed, cancellationToken).ConfigureAwait(false);
        if (consumedTokenResult.IsFailure)
        {
            // The execution row is already consumed and unresumable either way — this only affects
            // whether a later replay is reported as "already consumed" instead of "never existed" once
            // a future sweep removes the execution row. Logged, not fatal to this call.
            DatabaseExecutionStoreLog.ConsumedTokenWriteFailed(_logger, entity.Id);
        }

        var flow = await _flows.Get(entity.FlowName, cancellationToken).ConfigureAwait(false);
        if (flow.IsFailure)
            return flow.ToNewResult<ExecutionRecord>();

        if (flow.Value!.SecretManagerName is not { } secretManagerName || flow.Value.SecretKeyName is not { } secretKeyName)
            return GenericResult<ExecutionRecord>.Failure(DatabaseExecutionStoreLog.NoExecutionKey(_logger, entity.FlowName));

        var plaintext = await _encryption.Execute(
            new DecryptCommand
            {
                SecretManagerName = secretManagerName,
                SecretKeyName = secretKeyName,
                Ciphertext = entity.ContextData,
            },
            cancellationToken).ConfigureAwait(false);
        if (plaintext.IsFailure)
            return plaintext.ToNewResult<ExecutionRecord>();

        var contextData = JsonSerializer.Deserialize<AuthenticationContextData>(plaintext.Value!);
        if (contextData is null)
        {
            return GenericResult<ExecutionRecord>.Failure(
                DatabaseExecutionStoreLog.ContextDataUnreadable(_logger, entity.Id));
        }

        var context = contextData.ToContext();
        if (context.IsFailure)
            return context.ToNewResult<ExecutionRecord>();

        DatabaseExecutionStoreLog.Consumed(_logger, entity.Id, entity.FlowName);
        return GenericResult<ExecutionRecord>.Success(new ExecutionRecord
        {
            Id = entity.Id,
            FlowName = entity.FlowName,
            Context = context.Value!,
            CurrentStepIndex = entity.CurrentStepIndex,
            ExpiresAt = entity.ExpiresAt,
        });
    }

    private static string Base64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
