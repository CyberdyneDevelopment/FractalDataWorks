using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Fdw.Data;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Execution;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.SecretProtection.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Execution;

/// <summary>
/// Holds suspended flows in <c>auth.AuthenticationExecution</c> (ConfigurationDb) instead of process
/// memory.
/// </summary>
/// <remarks>
/// Correct behind a load balancer, where <see cref="InMemoryExecutionStore"/> is not: a caller can
/// return to a different instance than the one that suspended them. Consumption mirrors the DDL's own
/// contract (see <c>auth.AuthenticationExecution</c>'s extended property) — a single atomic UPDATE
/// guarded by the unique index on <c>ResumeTokenHash</c>, never a delete, so two concurrent resumes
/// cannot both proceed. A found-but-already-consumed row and an expired row both fail exactly as a
/// missing one does, for the same reason <see cref="InMemoryExecutionStore"/> already gives: telling a
/// caller a token was real, only spent or stale, is the one piece of information a reject must not
/// leak. <c>auth.ConsumedResumeToken</c> is written best-effort after a successful consume — durability
/// for after the execution row is eventually swept, not the safety mechanism itself, which is the
/// atomic UPDATE alone.
/// </remarks>
public sealed class DatabaseExecutionStore : IAuthenticationExecutionStore
{
    // Matches AuthenticationStepTypes.ConfigurationConnection's default -- the same physical
    // ConfigurationDb the sibling AuthenticationFlow/-Step providers already read "auth" from.
    private const string DataStoreName = "PlatformConfiguration";
    private const string PathName = "auth";
    private const string ExecutionContainer = "AuthenticationExecution";
    private const string ConsumedTokenContainer = "ConsumedResumeToken";

    private readonly IDataGatewayProvider _dataGateways;
    private readonly ISecretProtector _protector;
    private readonly ILogger _logger;

    private IDataGateway Gateway => _dataGateways.ByName("Main");

    /// <summary>Initializes a new instance of the <see cref="DatabaseExecutionStore"/> class.</summary>
    /// <param name="dataGateways">Supplies the gateway ConfigurationDb is reached through.</param>
    /// <param name="protector">Encrypts and decrypts a suspended execution's context.</param>
    /// <param name="logger">The logger.</param>
    public DatabaseExecutionStore(
        IDataGatewayProvider dataGateways, ISecretProtector protector, ILogger<DatabaseExecutionStore>? logger = null)
    {
        _dataGateways = dataGateways ?? throw new ArgumentNullException(nameof(dataGateways));
        _protector = protector ?? throw new ArgumentNullException(nameof(protector));
        _logger = logger ?? NullLogger<DatabaseExecutionStore>.Instance;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<string>> Suspend(ExecutionRecord record, CancellationToken cancellationToken = default)
    {
        if (record is null)
            return GenericResult<string>.Failure(ExecutionStoreLog.RecordMissing(_logger));

        var token = Base64Url(RandomNumberGenerator.GetBytes(32));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));

        var protectResult = _protector.Protect(JsonSerializer.SerializeToUtf8Bytes(ToDto(record.Context)));
        if (protectResult.IsFailure)
            return GenericResult<string>.Failure(
                ExecutionStoreLog.ProtectionFailed(_logger, "Protect", protectResult.CurrentMessage ?? "protect failed"));

        var entity = new ExecutionRecordEntity
        {
            Id = record.Id,
            FlowName = record.FlowName,
            ResumeTokenHash = hash,
            ContextData = protectResult.Value!,
            CurrentStepIndex = record.CurrentStepIndex,
            ExpiresAt = record.ExpiresAt,
            ConsumedAt = null,
        };

        var insertCall = Insert.Into<ExecutionRecordEntity>(ExecutionContainer)
            .DataStore(DataStoreName).Path(PathName).Value(entity);

        var insertResult = await Gateway.Execute<int>(insertCall, cancellationToken).ConfigureAwait(false);
        if (insertResult.IsFailure)
            return GenericResult<string>.Failure(
                ExecutionStoreLog.PersistenceFailed(_logger, "Suspend", insertResult.CurrentMessage ?? "insert failed"));

        ExecutionStoreLog.Suspended(_logger, record.Id, record.FlowName, record.CurrentStepIndex);
        return GenericResult<string>.Success(token);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<ExecutionRecord>> TryConsume(string resumeToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(resumeToken))
            return GenericResult<ExecutionRecord>.Failure(ExecutionStoreLog.TokenMissing(_logger));

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(resumeToken));

        var rowResult = await ResolveAndConsumeRow(hash, cancellationToken).ConfigureAwait(false);
        if (rowResult.IsFailure)
            return rowResult.ToNewResult<ExecutionRecord>();

        return await FinalizeConsumed(rowResult.Value!, hash, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Finds the row by its token hash and atomically flips it consumed. Isolated from
    /// <see cref="TryConsume"/> to keep that method's branching within FDW007's threshold.
    /// </summary>
    private async Task<IGenericResult<ExecutionRecordEntity>> ResolveAndConsumeRow(
        byte[] hash, CancellationToken cancellationToken)
    {
        var queryCall = Query.From<ExecutionRecordEntity>(DataStoreName, PathName, ExecutionContainer)
            .Where(r => r.ResumeTokenHash).Equal(hash)
            .Build();

        var queryResult = await Gateway.Execute<System.Collections.Generic.IEnumerable<ExecutionRecordEntity>>(
            queryCall, cancellationToken).ConfigureAwait(false);
        if (queryResult.IsFailure)
            return GenericResult<ExecutionRecordEntity>.Failure(
                ExecutionStoreLog.PersistenceFailed(_logger, "TryConsume", queryResult.CurrentMessage ?? "query failed"));

        var row = queryResult.Value?.FirstOrDefault();
        if (row is null)
            return GenericResult<ExecutionRecordEntity>.Failure(ExecutionStoreLog.NotResumable(_logger));

        if (row.ConsumedAt is not null)
        {
            ExecutionStoreLog.AlreadyConsumed(_logger, row.Id);
            return GenericResult<ExecutionRecordEntity>.Failure(ExecutionStoreLog.NotResumable(_logger));
        }

        if (row.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            ExecutionStoreLog.Expired(_logger, row.Id);
            return GenericResult<ExecutionRecordEntity>.Failure(ExecutionStoreLog.NotResumable(_logger));
        }

        row.ConsumedAt = DateTimeOffset.UtcNow;

        var updateCall = Update.In<ExecutionRecordEntity>(ExecutionContainer)
            .DataStore(DataStoreName).Path(PathName)
            .Where(nameof(ExecutionRecordEntity.ResumeTokenHash), hash)
            .Where(nameof(ExecutionRecordEntity.ConsumedAt), new IsNullOperator(), null)
            .Value(row);

        var updateResult = await Gateway.Execute<int>(updateCall, cancellationToken).ConfigureAwait(false);
        if (updateResult.IsFailure)
            return GenericResult<ExecutionRecordEntity>.Failure(
                ExecutionStoreLog.PersistenceFailed(_logger, "TryConsume", updateResult.CurrentMessage ?? "update failed"));

        if (updateResult.Value != 1)
        {
            ExecutionStoreLog.ConsumeRaceLost(_logger, row.Id);
            return GenericResult<ExecutionRecordEntity>.Failure(ExecutionStoreLog.NotResumable(_logger));
        }

        return GenericResult<ExecutionRecordEntity>.Success(row);
    }

    /// <summary>Decrypts a newly-consumed row's context and records its replay tombstone.</summary>
    private async Task<IGenericResult<ExecutionRecord>> FinalizeConsumed(
        ExecutionRecordEntity row, byte[] hash, CancellationToken cancellationToken)
    {
        var unprotectResult = _protector.Unprotect(row.ContextData);
        if (unprotectResult.IsFailure)
            return GenericResult<ExecutionRecord>.Failure(
                ExecutionStoreLog.ProtectionFailed(_logger, "Unprotect", unprotectResult.CurrentMessage ?? "unprotect failed"));

        var dto = JsonSerializer.Deserialize<ContextDataDto>(unprotectResult.Value!)
            ?? throw new InvalidOperationException("Decrypted execution context deserialized to null.");

        // Best-effort replay tombstone: durability for after this row is eventually swept, not the
        // primary single-use guarantee (the atomic update in ResolveAndConsumeRow already is that).
        var tombstoneCall = Insert.Into<ConsumedResumeTokenEntity>(ConsumedTokenContainer)
            .DataStore(DataStoreName).Path(PathName)
            .Value(new ConsumedResumeTokenEntity { ResumeTokenHash = hash, ExpiresAt = row.ExpiresAt });

        var tombstoneResult = await Gateway.Execute<int>(tombstoneCall, cancellationToken).ConfigureAwait(false);
        if (tombstoneResult.IsFailure)
            ExecutionStoreLog.ReplayTombstoneWriteFailed(_logger, row.Id, tombstoneResult.CurrentMessage ?? "insert failed");

        ExecutionStoreLog.Consumed(_logger, row.Id, row.FlowName);

        return GenericResult<ExecutionRecord>.Success(new ExecutionRecord
        {
            Id = row.Id,
            FlowName = row.FlowName,
            Context = FromDto(dto),
            CurrentStepIndex = row.CurrentStepIndex,
            ExpiresAt = row.ExpiresAt,
        });
    }

    private static string Base64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static ContextDataDto ToDto(AuthenticationContext context) => new()
    {
        Subject = context.Subject is null ? null : new SubjectDto
        {
            Issuer = context.Subject.Issuer,
            SubjectId = context.Subject.SubjectId,
            AuthenticatedAt = context.Subject.AuthenticatedAt,
        },
        Principal = context.Principal is null ? null : new PrincipalDto
        {
            Id = context.Principal.Id,
            TenantId = context.Principal.TenantId,
        },
        Claims = [.. context.Claims.Claims.Select(c => new ClaimDto
        {
            Type = c.Type,
            Value = c.Value,
            Source = c.Source.Name,
            Issuer = c.Issuer,
        })],
        Decision = context.Decision is null ? null : new DecisionDto
        {
            Permitted = context.Decision.Permitted,
            Reason = context.Decision.Reason,
        },
        AchievedMethods = [.. context.AchievedMethods],
        AchievedAcr = context.AchievedAcr,
    };

    private static AuthenticationContext FromDto(ContextDataDto dto) => new()
    {
        Subject = dto.Subject is null ? null : new Subject
        {
            Issuer = dto.Subject.Issuer,
            SubjectId = dto.Subject.SubjectId,
            AuthenticatedAt = dto.Subject.AuthenticatedAt,
        },
        Principal = dto.Principal is null ? null : new Principal
        {
            Id = dto.Principal.Id,
            TenantId = dto.Principal.TenantId,
        },
        Claims = ClaimSet.Empty.Add(dto.Claims.Select(ToClaim)),
        Decision = dto.Decision is null ? null : new Decision
        {
            Permitted = dto.Decision.Permitted,
            Reason = dto.Decision.Reason,
        },
        AchievedMethods = [.. dto.AchievedMethods],
        AchievedAcr = dto.AchievedAcr,
    };

    private static Claim ToClaim(ClaimDto dto)
    {
        var source = ClaimSources.ByName(dto.Source);
        if (source == ClaimSources.NotFound)
            throw new InvalidOperationException(
                $"Stored claim source '{dto.Source}' no longer resolves against ClaimSources.");

        return new Claim { Type = dto.Type, Value = dto.Value, Source = source, Issuer = dto.Issuer };
    }
}
