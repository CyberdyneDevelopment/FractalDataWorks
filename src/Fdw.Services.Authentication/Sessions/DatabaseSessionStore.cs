using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Claims.Abstractions;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Fdw.Data;
using Fdw.Results;
using Fdw.Security.Encryption;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Sessions;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Flow;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Settings;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Sessions;

/// <summary>
/// Holds server-side sessions in <c>auth.Session</c> (AuthDb), so a session is honoured by whichever
/// instance a request reaches (FDW-903).
/// </summary>
/// <remarks>
/// <para>
/// The session id is 256 random bits and is hashed with unkeyed SHA-256 before it reaches a query,
/// as resume tokens and sign-in codes are: that much entropy leaves a keyed hash nothing to add.
/// The claims are encrypted under the key the issuing flow names, the same key its suspended
/// executions and sign-in codes use, so a row inserted by anyone without that key cannot be read as a
/// session.
/// </para>
/// <para>
/// How long a session lives is the deployment's decision, read from two server settings each time a
/// session is created: <c>SessionTimeoutMinutes</c> (idle) and <c>SessionAbsoluteLifetimeHours</c>
/// (absolute). Neither has a default. The absolute end is also capped at the claims' own
/// <see cref="IssuanceRequest.ValidUntil"/>, exactly as a bearer token's expiry is (FDW-862).
/// </para>
/// </remarks>
public sealed class DatabaseSessionStore : ISessionStore
{
    private const string PathName = "auth";
    private const string Container = "Session";

    // How many sessions deep a chain of acting may run before the rows are taken to be broken. A person steps down a few roles, not dozens.
    private const int MaxChainDepth = 32;

    private readonly IConfigurationGatewayProvider _gateways;
    private readonly IAuthenticationContextAccessor _authContextAccessor;
    private readonly IEncryptionCommandExecutor _encryption;
    private readonly IAuthenticationFlowProvider _flows;
    private readonly IServerSettingConfigurationProvider _serverSettings;
    private readonly TimeSpan _useWriteCap;
    private readonly ILogger<DatabaseSessionStore> _logger;

    /// <summary>Initializes a new instance of the <see cref="DatabaseSessionStore"/> class.</summary>
    /// <param name="gateways">Holds the gateway for the host-declared AuthDb connection, the only path to the authentication state (FDW-1084).</param>
    /// <param name="authContextAccessor">
    /// Lets each call run as the system principal: a session is created before its caller is a
    /// principal, and resolved before this request has one.
    /// </param>
    /// <param name="encryption">Encrypts and decrypts the stored claims.</param>
    /// <param name="flows">Names, per flow, the key that encrypts a session's claims.</param>
    /// <param name="serverSettings">Supplies the idle and absolute session lifetimes.</param>
    /// <param name="useWriteCap">
    /// The longest a session's stored idle expiry may fall behind its last request before it is written again;
    /// stated where the host registers the store, and never longer than half the session's own idle timeout.
    /// </param>
    /// <param name="logger">The logger.</param>
    public DatabaseSessionStore(
        IConfigurationGatewayProvider gateways,
        IAuthenticationContextAccessor authContextAccessor,
        IEncryptionCommandExecutor encryption,
        IAuthenticationFlowProvider flows,
        IServerSettingConfigurationProvider serverSettings,
        TimeSpan useWriteCap,
        ILogger<DatabaseSessionStore>? logger = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(useWriteCap, TimeSpan.Zero);
        _useWriteCap = useWriteCap;
        _gateways = gateways ?? throw new ArgumentNullException(nameof(gateways));
        _authContextAccessor = authContextAccessor ?? throw new ArgumentNullException(nameof(authContextAccessor));
        _encryption = encryption ?? throw new ArgumentNullException(nameof(encryption));
        _flows = flows ?? throw new ArgumentNullException(nameof(flows));
        _serverSettings = serverSettings ?? throw new ArgumentNullException(nameof(serverSettings));
        _logger = logger ?? NullLogger<DatabaseSessionStore>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IssuedToken>> Create(
        string serviceName, string flowName, IssuanceRequest assertion, CancellationToken cancellationToken = default)
    {
        var created = await CreateSession(serviceName, flowName, assertion, null, cancellationToken).ConfigureAwait(false);
        return created.IsFailure
            ? created.ToNewResult<IssuedToken>()
            : GenericResult<IssuedToken>.Success(created.Value.Token);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IssuedToken>> Act(
        Guid parentSessionId, IssuanceRequest assertion, CancellationToken cancellationToken = default)
    {
        if (assertion is null)
            return GenericResult<IssuedToken>.Failure(SessionLog.CreateArgumentsMissing(_logger));

        using var scope = new SystemAuthenticationContextScope(_authContextAccessor);

        var read = await _gateways.Execute<IEnumerable<SessionEntity>>(
            AuthenticationStepTypes.StateConnection,
            Query.From<SessionEntity>(AuthenticationStepTypes.StateConnection, PathName, Container)
                .Where(r => r.Id).Equal(parentSessionId)
                .Build(),
            cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
            return read.ToNewResult<IssuedToken>();

        var parentRow = read.Value!.FirstOrDefault();
        var refusal = parentRow is null ? "no session has this id" : NotLive(parentRow, parentRow.ServiceName, DateTimeOffset.UtcNow);
        if (refusal is not null)
            return GenericResult<IssuedToken>.Failure(SessionLog.SessionNotLive(_logger, "(act)", refusal));

        var parent = read.Value!.First();

        // The acting session never outlives the session it came from, and the claims it asserts carry that end.
        var capped = assertion with
        {
            ValidUntil = assertion.ValidUntil is { } validUntil && validUntil < parent.AbsoluteExpiresAt ? validUntil : parent.AbsoluteExpiresAt,
        };

        var created = await CreateSession(parent.ServiceName, parent.FlowName, capped, parent.Id, cancellationToken).ConfigureAwait(false);
        if (created.IsFailure)
            return created.ToNewResult<IssuedToken>();

        // The parent sleeps only once the session above it exists, and the new one is revoked again if it cannot, so a failure leaves
        // the person in the session they were in and never in two live ones.
        var slept = await Sleep(parent.Id, cancellationToken).ConfigureAwait(false);
        if (slept.IsFailure)
        {
            var undone = await Revoke(created.Value.Id, cancellationToken).ConfigureAwait(false);
            if (undone.IsFailure)
                SessionLog.SessionWriteFailed(_logger, "undo an acting session");

            return slept.ToNewResult<IssuedToken>();
        }

        return GenericResult<IssuedToken>.Success(created.Value.Token);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IssuedToken>> Return(Guid actingSessionId, int step, CancellationToken cancellationToken = default)
    {
        var ancestry = await Ancestry(actingSessionId, cancellationToken).ConfigureAwait(false);
        if (ancestry.IsFailure)
            return ancestry.ToNewResult<IssuedToken>();

        var current = ancestry.Value![0];
        if (NotLive(current, current.ServiceName, DateTimeOffset.UtcNow) is { } notLive)
            return GenericResult<IssuedToken>.Failure(SessionLog.SessionNotLive(_logger, "(return)", notLive));

        var plan = ActingReturn.Of([.. ancestry.Value!.Select(row => row.Id)], step, out var refusal);
        if (plan is null)
            return GenericResult<IssuedToken>.Failure(SessionLog.ReturnRefused(_logger, refusal!));

        // Ended top first and before the wake, so a failure part way never leaves two sessions awake: at worst the person has no live
        // session left in the chain and signs in again.
        foreach (var above in plan.Revoke)
        {
            var revoked = await Revoke(above, cancellationToken).ConfigureAwait(false);
            if (revoked.IsFailure)
                return revoked.ToNewResult<IssuedToken>();
        }

        var woken = await Wake(plan.Wake, cancellationToken).ConfigureAwait(false);
        if (woken.IsSuccess)
            SessionLog.ActingReturned(_logger, plan.Wake, plan.Revoke.Count);

        return woken;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<ActingStep>>> Chain(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var ancestry = await Ancestry(sessionId, cancellationToken).ConfigureAwait(false);
        if (ancestry.IsFailure)
            return ancestry.ToNewResult<IReadOnlyList<ActingStep>>();

        var steps = new List<ActingStep>(ancestry.Value!.Count);
        for (var i = ancestry.Value!.Count - 1; i >= 0; i--)
        {
            var row = ancestry.Value![i];
            var assertion = await ReadAssertion(row, cancellationToken).ConfigureAwait(false);
            if (assertion.IsFailure)
                return assertion.ToNewResult<IReadOnlyList<ActingStep>>();

            steps.Add(new ActingStep
            {
                Step = steps.Count,
                SessionId = row.Id,
                Role = assertion.Value!.Claims.TryGetValue(ClaimDefinitions.actingRole.Name, out var role) && role.Count > 0 ? role[0] : null,
                StartedAt = row.CreatedAt,
                IsCurrent = i == 0,
            });
        }

        return GenericResult<IReadOnlyList<ActingStep>>.Success(steps);
    }

    // The session and every session it was started from, current first, ending at the person's own. A parent that cannot be read, or a
    // chain that loops or runs past MaxChainDepth, is a fault in the rows and is refused rather than cut short.
    private async Task<IGenericResult<IReadOnlyList<SessionEntity>>> Ancestry(Guid sessionId, CancellationToken cancellationToken)
    {
        using var scope = new SystemAuthenticationContextScope(_authContextAccessor);

        var chain = new List<SessionEntity>();
        var seen = new HashSet<Guid>();
        Guid? next = sessionId;
        while (next is { } id)
        {
            if (chain.Count >= MaxChainDepth || !seen.Add(id))
                return GenericResult<IReadOnlyList<SessionEntity>>.Failure(SessionLog.ChainBroken(_logger, sessionId));

            var read = await _gateways.Execute<IEnumerable<SessionEntity>>(
                AuthenticationStepTypes.StateConnection,
                Query.From<SessionEntity>(AuthenticationStepTypes.StateConnection, PathName, Container)
                    .Where(r => r.Id).Equal(id)
                    .Build(),
                cancellationToken).ConfigureAwait(false);
            if (read.IsFailure)
                return read.ToNewResult<IReadOnlyList<SessionEntity>>();

            if (read.Value!.FirstOrDefault() is not { } row)
                return GenericResult<IReadOnlyList<SessionEntity>>.Failure(SessionLog.SessionNotLive(_logger, "(chain)", "no session has this id"));

            chain.Add(row);
            next = row.ParentSessionId;
        }

        return GenericResult<IReadOnlyList<SessionEntity>>.Success(chain);
    }

    private async Task<IGenericResult<IssuanceRequest>> ReadAssertion(SessionEntity entity, CancellationToken cancellationToken)
    {
        var key = await FlowKey(entity.FlowName, cancellationToken).ConfigureAwait(false);
        if (key.IsFailure)
            return key.ToNewResult<IssuanceRequest>();

        var plaintext = await _encryption.Execute(
            new DecryptCommand
            {
                SecretManagerName = key.Value.SecretManagerName,
                SecretKeyName = key.Value.SecretKeyName,
                Ciphertext = entity.ClaimsData,
            },
            cancellationToken).ConfigureAwait(false);
        if (plaintext.IsFailure)
            return plaintext.ToNewResult<IssuanceRequest>();

        return JsonSerializer.Deserialize<IssuanceRequest>(plaintext.Value!) is { } assertion
            ? GenericResult<IssuanceRequest>.Success(assertion)
            : GenericResult<IssuanceRequest>.Failure(SessionLog.ClaimsUnreadable(_logger, entity.Id));
    }

    private async Task<IGenericResult<(IssuedToken Token, Guid Id)>> CreateSession(
        string serviceName, string flowName, IssuanceRequest assertion, Guid? parentSessionId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(serviceName) || string.IsNullOrEmpty(flowName) || assertion is null)
            return GenericResult<(IssuedToken Token, Guid Id)>.Failure(SessionLog.CreateArgumentsMissing(_logger));

        using var scope = new SystemAuthenticationContextScope(_authContextAccessor);

        var key = await FlowKey(flowName, cancellationToken).ConfigureAwait(false);
        if (key.IsFailure)
            return key.ToNewResult<(IssuedToken Token, Guid Id)>();

        var idleMinutes = await PositiveSetting(SettingDefinitions.SessionTimeoutMinutes, cancellationToken).ConfigureAwait(false);
        if (idleMinutes.IsFailure)
            return idleMinutes.ToNewResult<(IssuedToken Token, Guid Id)>();

        var absoluteHours = await PositiveSetting(SettingDefinitions.SessionAbsoluteLifetimeHours, cancellationToken).ConfigureAwait(false);
        if (absoluteHours.IsFailure)
            return absoluteHours.ToNewResult<(IssuedToken Token, Guid Id)>();

        var now = DateTimeOffset.UtcNow;
        var absoluteExpiresAt = now.AddHours(absoluteHours.Value);

        // The configured lifetime is a ceiling, not a promise: a session never outlives the claims it
        // carries, the same rule a bearer token's expiry follows (FDW-862).
        if (assertion.ValidUntil is { } validUntil && validUntil < absoluteExpiresAt)
        {
            if (validUntil <= now)
                return GenericResult<(IssuedToken Token, Guid Id)>.Failure(
                    SessionLog.ClaimsAlreadyExpired(_logger, flowName, assertion.PrincipalId, validUntil));

            absoluteExpiresAt = validUntil;
        }

        var idleTimeout = TimeSpan.FromMinutes(idleMinutes.Value);
        var idleExpiresAt = Earlier(now.Add(idleTimeout), absoluteExpiresAt);

        var claims = await _encryption.Execute(
            new EncryptCommand
            {
                SecretManagerName = key.Value.SecretManagerName,
                SecretKeyName = key.Value.SecretKeyName,
                Plaintext = JsonSerializer.SerializeToUtf8Bytes(assertion),
            },
            cancellationToken).ConfigureAwait(false);
        if (claims.IsFailure)
            return claims.ToNewResult<(IssuedToken Token, Guid Id)>();

        var sessionId = Base64Url(RandomNumberGenerator.GetBytes(32));
        var id = Guid.CreateVersion7();

        var call = Insert.Into<SessionEntity>(Container)
            .DataStore(AuthenticationStepTypes.StateConnection)
            .Path(PathName)
            .Value(new SessionEntity
            {
                Id = id,
                SessionIdHash = Hash(sessionId),
                ServiceName = serviceName,
                FlowName = flowName,
                UserId = assertion.PrincipalId,
                ClaimsData = claims.Value!,
                IdleTimeoutSeconds = (int)idleTimeout.TotalSeconds,
                CreatedAt = now,
                LastSeenAt = now,
                IdleExpiresAt = idleExpiresAt,
                AbsoluteExpiresAt = absoluteExpiresAt,
                ParentSessionId = parentSessionId,
            });

        var written = await _gateways.Execute<int>(AuthenticationStepTypes.StateConnection, call, cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
        {
            SessionLog.SessionWriteFailed(_logger, "create a session");
            return written.ToNewResult<(IssuedToken Token, Guid Id)>();
        }

        SessionLog.SessionCreated(_logger, id, serviceName, flowName, absoluteExpiresAt);
        return GenericResult<(IssuedToken Token, Guid Id)>.Success((new IssuedToken
        {
            AccessToken = sessionId,
            TokenType = SessionRecord.TokenType,
            ExpiresAt = absoluteExpiresAt,
        }, id));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<SessionRecord>> Use(
        string sessionId, string serviceName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(sessionId))
            return GenericResult<SessionRecord>.Failure(SessionLog.SessionIdMissing(_logger, serviceName));

        using var scope = new SystemAuthenticationContextScope(_authContextAccessor);

        var read = await _gateways.Execute<IEnumerable<SessionEntity>>(
            AuthenticationStepTypes.StateConnection,
            Query.From<SessionEntity>(AuthenticationStepTypes.StateConnection, PathName, Container)
                .Where(r => r.SessionIdHash).Equal(Hash(sessionId))
                .Build(),
            cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
            return read.ToNewResult<SessionRecord>();

        var now = DateTimeOffset.UtcNow;
        if (NotLive(read.Value!.FirstOrDefault(), serviceName, now) is { } refusal)
            return GenericResult<SessionRecord>.Failure(SessionLog.SessionNotLive(_logger, serviceName, refusal));

        var entity = read.Value!.First();

        var readAssertion = await ReadAssertion(entity, cancellationToken).ConfigureAwait(false);
        if (readAssertion.IsFailure)
            return readAssertion.ToNewResult<SessionRecord>();

        var assertion = readAssertion.Value!;

        // Why the slide is not written on every request: every session-authenticated request made a write
        // command and a read command against the session table only to move LastSeenAt forward a few
        // seconds. The slide is written when the stored one has fallen far enough behind (WriteInterval),
        // so the idle expiry may be earlier than the last request plus the timeout by at most that
        // interval, never later. The read above already refused a revoked session; what the write also
        // guarded, a logout landing between that read and the write, is now the same window a read alone has.
        if (now - entity.LastSeenAt < WriteInterval(entity))
        {
            SessionLog.SessionUsed(_logger, entity.Id, serviceName, entity.IdleExpiresAt);
            return GenericResult<SessionRecord>.Success(ToRecord(entity, assertion));
        }

        // Guarded on the row still being live: a logout that lands between the read above and this
        // write must not be undone by it. Zero rows means exactly that race, so the request is refused.
        var idleExpiresAt = Earlier(now.AddSeconds(entity.IdleTimeoutSeconds), entity.AbsoluteExpiresAt);
        var slid = await _gateways.Execute<int>(
            AuthenticationStepTypes.StateConnection,
            Update.In<SessionUseUpdate>(Container)
                .DataStore(AuthenticationStepTypes.StateConnection)
                .Path(PathName)
                .Where(nameof(SessionEntity.Id), entity.Id)
                .Where(nameof(SessionEntity.RevokedAt), new IsNullOperator(), null)
                .Value(new SessionUseUpdate { LastSeenAt = now, IdleExpiresAt = idleExpiresAt }),
            cancellationToken).ConfigureAwait(false);
        if (slid.IsFailure)
        {
            SessionLog.SessionWriteFailed(_logger, "record a session's use");
            return slid.ToNewResult<SessionRecord>();
        }

        if (slid.Value != 1)
            return GenericResult<SessionRecord>.Failure(SessionLog.SessionNotLive(_logger, serviceName, "it was revoked while in use"));

        SessionLog.SessionUsed(_logger, entity.Id, serviceName, idleExpiresAt);
        return GenericResult<SessionRecord>.Success(ToRecord(entity, assertion));
    }

    // How far the stored slide may fall behind the last request before it is written again: the cap the host
    // registered the store with, or half the session's own idle timeout when that is shorter, so a short
    // session still slides before it can expire.
    private TimeSpan WriteInterval(SessionEntity entity)
    {
        var halfTimeout = TimeSpan.FromSeconds(entity.IdleTimeoutSeconds / 2.0);
        return halfTimeout < _useWriteCap ? halfTimeout : _useWriteCap;
    }

    private static SessionRecord ToRecord(SessionEntity entity, IssuanceRequest assertion) => new()
    {
        Id = entity.Id,
        ServiceName = entity.ServiceName,
        UserId = entity.UserId,
        CreatedAt = entity.CreatedAt,
        AbsoluteExpiresAt = entity.AbsoluteExpiresAt,
        Assertion = assertion,
        CsrfTokenHash = entity.CsrfTokenHash,
    };

    /// <inheritdoc />
    public async Task<IGenericResult> Revoke(Guid id, CancellationToken cancellationToken = default)
    {
        using var scope = new SystemAuthenticationContextScope(_authContextAccessor);

        var revoked = await _gateways.Execute<int>(
            AuthenticationStepTypes.StateConnection,
            Update.In<SessionRevokeUpdate>(Container)
                .DataStore(AuthenticationStepTypes.StateConnection)
                .Path(PathName)
                .Where(nameof(SessionEntity.Id), id)
                .Where(nameof(SessionEntity.RevokedAt), new IsNullOperator(), null)
                .Value(new SessionRevokeUpdate { RevokedAt = DateTimeOffset.UtcNow }),
            cancellationToken).ConfigureAwait(false);
        if (revoked.IsFailure)
        {
            SessionLog.SessionWriteFailed(_logger, "revoke a session");
            return revoked;
        }

        // Zero rows is a session that was already revoked or never existed. Reporting success for it
        // would say something was ended that was not.
        if (revoked.Value != 1)
            return GenericResult.Failure(SessionLog.SessionNotLive(_logger, "(revoke)", "no live session has this Id"));

        SessionLog.SessionRevoked(_logger, id);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Sleep(Guid id, CancellationToken cancellationToken = default)
    {
        using var scope = new SystemAuthenticationContextScope(_authContextAccessor);

        // Guarded on the row being live and awake: a session that was revoked, or is already asleep, is not put to sleep again.
        var slept = await _gateways.Execute<int>(
            AuthenticationStepTypes.StateConnection,
            Update.In<SessionSleepUpdate>(Container)
                .DataStore(AuthenticationStepTypes.StateConnection)
                .Path(PathName)
                .Where(nameof(SessionEntity.Id), id)
                .Where(nameof(SessionEntity.RevokedAt), new IsNullOperator(), null)
                .Where(nameof(SessionEntity.AsleepAt), new IsNullOperator(), null)
                .Value(new SessionSleepUpdate { AsleepAt = DateTimeOffset.UtcNow }),
            cancellationToken).ConfigureAwait(false);
        if (slept.IsFailure)
        {
            SessionLog.SessionWriteFailed(_logger, "put a session to sleep");
            return slept;
        }

        if (slept.Value != 1)
            return GenericResult.Failure(SessionLog.SessionNotLive(_logger, "(sleep)", "no live, awake session has this Id"));

        SessionLog.SessionSlept(_logger, id);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IssuedToken>> Wake(Guid id, CancellationToken cancellationToken = default)
    {
        using var scope = new SystemAuthenticationContextScope(_authContextAccessor);

        var read = await _gateways.Execute<IEnumerable<SessionEntity>>(
            AuthenticationStepTypes.StateConnection,
            Query.From<SessionEntity>(AuthenticationStepTypes.StateConnection, PathName, Container)
                .Where(r => r.Id).Equal(id)
                .Build(),
            cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
            return read.ToNewResult<IssuedToken>();

        var now = DateTimeOffset.UtcNow;
        if (NotWakeable(read.Value!.FirstOrDefault(), now) is { } refusal)
            return GenericResult<IssuedToken>.Failure(SessionLog.SessionNotLive(_logger, "(wake)", refusal));

        var entity = read.Value!.First();

        // Time asleep is not idleness: the idle clock starts again from the wake, never past the absolute end.
        var idleExpiresAt = Earlier(now.AddSeconds(entity.IdleTimeoutSeconds), entity.AbsoluteExpiresAt);
        var sessionId = Base64Url(RandomNumberGenerator.GetBytes(32));

        // Guarded on the row still being live and asleep: a revoke or a second wake landing between the read and this write wins.
        var woken = await _gateways.Execute<int>(
            AuthenticationStepTypes.StateConnection,
            Update.In<SessionWakeUpdate>(Container)
                .DataStore(AuthenticationStepTypes.StateConnection)
                .Path(PathName)
                .Where(nameof(SessionEntity.Id), id)
                .Where(nameof(SessionEntity.RevokedAt), new IsNullOperator(), null)
                .Where(nameof(SessionEntity.AsleepAt), new IsNotNullOperator(), null)
                .Value(new SessionWakeUpdate
                {
                    SessionIdHash = Hash(sessionId),
                    AsleepAt = null,
                    CsrfTokenHash = null,
                    LastSeenAt = now,
                    IdleExpiresAt = idleExpiresAt,
                }),
            cancellationToken).ConfigureAwait(false);
        if (woken.IsFailure)
        {
            SessionLog.SessionWriteFailed(_logger, "wake a session");
            return woken.ToNewResult<IssuedToken>();
        }

        if (woken.Value != 1)
            return GenericResult<IssuedToken>.Failure(SessionLog.SessionNotLive(_logger, "(wake)", "it changed while it was being woken"));

        SessionLog.SessionWoken(_logger, id, idleExpiresAt);
        return GenericResult<IssuedToken>.Success(new IssuedToken
        {
            AccessToken = sessionId,
            TokenType = SessionRecord.TokenType,
            ExpiresAt = entity.AbsoluteExpiresAt,
        });
    }

    /// <inheritdoc />
    public async Task<IGenericResult<string>> IssueCsrfToken(Guid id, CancellationToken cancellationToken = default)
    {
        using var scope = new SystemAuthenticationContextScope(_authContextAccessor);

        var token = Base64Url(RandomNumberGenerator.GetBytes(32));
        var issued = await _gateways.Execute<int>(
            AuthenticationStepTypes.StateConnection,
            Update.In<SessionCsrfUpdate>(Container)
                .DataStore(AuthenticationStepTypes.StateConnection)
                .Path(PathName)
                .Where(nameof(SessionEntity.Id), id)
                .Where(nameof(SessionEntity.RevokedAt), new IsNullOperator(), null)
                .Value(new SessionCsrfUpdate { CsrfTokenHash = Hash(token) }),
            cancellationToken).ConfigureAwait(false);
        if (issued.IsFailure)
        {
            SessionLog.SessionWriteFailed(_logger, "issue a CSRF token");
            return issued.ToNewResult<string>();
        }

        if (issued.Value != 1)
            return GenericResult<string>.Failure(SessionLog.SessionNotLive(_logger, "(csrf)", "no live session has this Id"));

        SessionLog.CsrfTokenIssued(_logger, id);
        return GenericResult<string>.Success(token);
    }

    /// <summary>Hashes a session id or CSRF token the way every stored copy of one is hashed.</summary>
    /// <param name="value">The id or token.</param>
    /// <returns>Its unkeyed SHA-256.</returns>
    internal static byte[] Hash(string value) => SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));

    private async Task<IGenericResult<(string SecretManagerName, string SecretKeyName)>> FlowKey(
        string flowName, CancellationToken cancellationToken)
    {
        var flow = await _flows.Get(flowName, cancellationToken).ConfigureAwait(false);
        if (flow.IsFailure)
            return flow.ToNewResult<(string, string)>();

        return flow.Value!.SecretManagerName is { } secretManagerName && flow.Value.SecretKeyName is { } secretKeyName
            ? GenericResult<(string, string)>.Success((secretManagerName, secretKeyName))
            : GenericResult<(string, string)>.Failure(SessionLog.NoSessionKey(_logger, flowName));
    }

    private async Task<IGenericResult<int>> PositiveSetting(string settingName, CancellationToken cancellationToken)
    {
        var setting = await _serverSettings.Get(settingName, cancellationToken).ConfigureAwait(false);
        if (setting.IsFailure)
        {
            SessionLog.SettingUnusable(_logger, settingName, "it could not be read");
            return setting.ToNewResult<int>();
        }

        if (setting.Value is not { IsActive: true } row)
            return GenericResult<int>.Failure(SessionLog.SettingUnusable(_logger, settingName, "it is not active"));

        return int.TryParse(row.SettingValue, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value > 0
            ? GenericResult<int>.Success(value)
            : GenericResult<int>.Failure(SessionLog.SettingUnusable(
                _logger, settingName, $"'{row.SettingValue}' is not a whole number greater than zero"));
    }

    // Why a session cannot be used, for the operator's log; null when it is live for this service.
    internal static string? NotLive(SessionEntity? row, string serviceName, DateTimeOffset now) => row switch
    {
        null => "no session has this id",
        { } live when !string.Equals(live.ServiceName, serviceName, StringComparison.Ordinal)
            => $"it belongs to service '{live.ServiceName}'",
        { RevokedAt: not null } => "it was revoked",
        { AsleepAt: not null } => "it is asleep: a session started from it is in use",
        { } live when live.AbsoluteExpiresAt <= now => "it reached its absolute end",
        { } live when live.IdleExpiresAt <= now => "it was idle past its timeout",
        _ => null,
    };

    // Why a session cannot be woken, for the operator's log; null when it is asleep and can be. Idleness is not checked: a session
    // sleeps for as long as the session above it is in use, and its idle clock starts again at the wake.
    internal static string? NotWakeable(SessionEntity? row, DateTimeOffset now) => row switch
    {
        null => "no session has this id",
        { RevokedAt: not null } => "it was revoked",
        { } live when live.AbsoluteExpiresAt <= now => "it reached its absolute end",
        { AsleepAt: null } => "it is not asleep",
        _ => null,
    };

    private static DateTimeOffset Earlier(DateTimeOffset left, DateTimeOffset right) => left < right ? left : right;

    private static string Base64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
