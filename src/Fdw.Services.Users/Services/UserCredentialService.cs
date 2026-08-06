using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Security.Hashing;
using Fdw.Services.Credentials.Abstractions;
using Fdw.Services.Credentials.Abstractions.Outcomes;
using Fdw.Services.Users.Abstractions;
using Fdw.Services.Users.Configuration;
using Fdw.Services.Users.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Users.Services;

/// <summary>
/// The credential EDGE. Hashes the plaintext on arrival (KDF with the user's salt), discards the
/// plaintext, hands the derived hash to the password <see cref="ICredentialService"/> (which peppers +
/// compares inside the vault), then composes the caller-facing <see cref="ICredentialOutcome"/> from
/// non-secret policy metadata (lockout counter, password age, must-change flag).
/// </summary>
/// <remarks>
/// Plaintext never reaches the vault and is never stored or logged. The vault produces only
/// Match/NoMatch; expiry, must-change, and lockout are composed HERE from non-secret data on the user
/// record (README §5/§6/§7). The negative path runs the same KDF against a fixed decoy salt so timing
/// does not enumerate accounts, and every failure logs a single uniform "denied" message.
/// Reads and writes of usr.Users security columns now go through <see cref="UserConfigurationProvider"/>
/// instead of directly against the configuration gateway.
/// </remarks>
public sealed class UserCredentialService : IUserCredentialService
{
    private const string PasswordSecretType = "Password";

    // Why: a fixed, valid Base64 16-byte salt used ONLY on the negative path so the KDF runs with the
    // same cost whether or not the user/salt exists (anti-enumeration, README §6).
    private const string DecoySaltBase64 = "ZmR3LWRlY295LXNhbHQwMQ==";

    private readonly ICredentialServiceProvider _credentialServiceProvider;
    private readonly IOptions<UsersServiceOptions> _usersOptions;
    private readonly IOptions<PasswordPolicyOptions> _passwordPolicy;
    // Why: all usr.Users reads and writes go through UserConfigurationProvider — single owner of
    // the usr.Users gateway path.
    private readonly UserConfigurationProvider _userProvider;
    private readonly ILogger<UserCredentialService> _logger;

    // Why: the credential service is resolved once lazily so a missing/misconfigured service name
    // surfaces as a structured failure on first use rather than crashing the DI container at startup.
    private ICredentialService? _credentialService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserCredentialService"/> class.
    /// </summary>
    public UserCredentialService(
        ICredentialServiceProvider credentialServiceProvider,
        IOptions<UsersServiceOptions> usersOptions,
        IOptions<PasswordPolicyOptions> passwordPolicy,
        UserConfigurationProvider userProvider,
        ILogger<UserCredentialService>? logger = null)
    {
        _credentialServiceProvider = credentialServiceProvider ?? throw new ArgumentNullException(nameof(credentialServiceProvider));
        _usersOptions = usersOptions ?? throw new ArgumentNullException(nameof(usersOptions));
        _passwordPolicy = passwordPolicy ?? throw new ArgumentNullException(nameof(passwordPolicy));
        _userProvider = userProvider ?? throw new ArgumentNullException(nameof(userProvider));
        _logger = logger ?? NullLogger<UserCredentialService>.Instance;
    }

    /// <inheritdoc />
#pragma warning disable MA0051 // Why: sequential fail-loud + uniform-timing branches read top-to-bottom; splitting hurts the anti-enumeration flow.
    public async Task<IGenericResult<ICredentialOutcome>> Verify(
        Guid userId, string secretType, string plaintext, CancellationToken cancellationToken = default)
    {
        // Why: only the password secret is vault-backed by this edge.
        if (!string.Equals(secretType, PasswordSecretType, StringComparison.OrdinalIgnoreCase))
            return GenericResult<ICredentialOutcome>.Failure(UserLog.SecretTypeNotSupported(_logger, userId, secretType));

        var policy = _passwordPolicy.Value;
        if (policy.MaxFailedLoginAttempts > 0 && policy.LockoutDurationMinutes <= 0)
            return GenericResult<ICredentialOutcome>.Failure(
                UserLog.PasswordPolicyInvalid(_logger, "MaxFailedLoginAttempts is set but LockoutDurationMinutes is not positive"));

        var serviceResult = await ResolveCredentialService(userId, secretType, cancellationToken).ConfigureAwait(false);
        if (!serviceResult.IsSuccess || serviceResult.Value is null)
            return serviceResult.ToNewResult<ICredentialOutcome>();

        var userCfgResult = await GetUserSecurity(userId, cancellationToken).ConfigureAwait(false);

        // Why: a provider/gateway FAILURE (e.g. a transient DB error) is an infrastructure outage, not an
        // "unknown account" — fail loud here so it is never masked as NoMatch via the decoy path below.
        if (!userCfgResult.IsSuccess)
            return GenericResult<ICredentialOutcome>.Failure(UserLog.UserSecurityLookupFailed(_logger, userId));

        var userCfg = userCfgResult.Value;

        // Why: §6 — no user/salt on file (a SUCCESSFUL lookup that found no row) runs the SAME KDF against
        // a fixed decoy salt, then returns the generic NoMatch, so an attacker cannot distinguish "unknown
        // account" from "wrong password".
        if (userCfg is null || string.IsNullOrEmpty(userCfg.Salt) || string.IsNullOrWhiteSpace(userCfg.AlgorithmName))
            return RunDecoyAndDeny(userId, plaintext, policy);

        // Why: verify with the algorithm the secret was CREATED with (enables upgrade-on-verify later).
        var algorithm = PasswordHashAlgorithms.ByName(userCfg.AlgorithmName);
        if (algorithm == PasswordHashAlgorithms.NotFound)
            return GenericResult<ICredentialOutcome>.Failure(
                UserLog.VaultAlgorithmNotFound(_logger, userId, secretType, userCfg.AlgorithmName));

        // Why: hash-on-arrival — derive the key from the user's salt, then the plaintext is no longer used.
        var derivedHash = algorithm.DeriveKey(plaintext, userCfg.Salt);

        var validate = await serviceResult.Value.Validate(userId, derivedHash, cancellationToken).ConfigureAwait(false);
        if (!validate.IsSuccess || validate.Value is null)
            return validate;

        var now = DateTimeOffset.UtcNow;

        if (!validate.Value.GrantsAccess)
            return await OnNoMatch(userId, userCfg, policy, now, cancellationToken).ConfigureAwait(false);

        return await ComposeSuccessOutcome(userId, userCfg, policy, now, cancellationToken).ConfigureAwait(false);
    }
#pragma warning restore MA0051

    // Why: §6 negative path — run the SAME KDF (against a fixed decoy salt) so a missing user/salt costs
    // the same time as a real verify, then log the uniform denial and return the generic NoMatch.
    private IGenericResult<ICredentialOutcome> RunDecoyAndDeny(Guid userId, string plaintext, PasswordPolicyOptions policy)
    {
        var decoyAlgorithm = PasswordHashAlgorithms.ByName(policy.PasswordHashAlgorithm);
        if (decoyAlgorithm != PasswordHashAlgorithms.NotFound)
            _ = decoyAlgorithm.DeriveKey(plaintext, DecoySaltBase64);

        UserLog.NoPasswordOnFileDecoy(_logger, userId);
        UserLog.AuthenticationDenied(_logger, userId);
        return GenericResult<ICredentialOutcome>.Success(CredentialOutcomes.ByName("NoMatch"));
    }

    // Why: the vault said Match — compose the final outcome from non-secret policy. A correct password on
    // a still-locked account is denied without resetting (serve the lockout window, README §7); otherwise
    // the counter resets (best-effort) and expiry / must-change is applied before granting access.
    private async Task<IGenericResult<ICredentialOutcome>> ComposeSuccessOutcome(
        Guid userId, UserConfiguration userCfg, PasswordPolicyOptions policy, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (userCfg.LockoutEnd is { } lockoutEnd && lockoutEnd > now)
        {
            UserLog.AuthenticationDenied(_logger, userId);
            return GenericResult<ICredentialOutcome>.Success(CredentialOutcomes.ByName("TooManyAttempts"));
        }

        if (userCfg.FailedLoginCount != 0 || userCfg.LockoutEnd is not null)
            await ResetLockout(userId, userCfg, cancellationToken).ConfigureAwait(false);

        if (policy.PasswordMaxAgeDays > 0
            && userCfg.LastPasswordChangedAt is { } changedAt
            && changedAt.AddDays(policy.PasswordMaxAgeDays) < now)
        {
            UserLog.CredentialOutcomeComposed(_logger, userId, "Expired");
            return GenericResult<ICredentialOutcome>.Success(CredentialOutcomes.ByName("Expired"));
        }

        if (userCfg.MustChangePasswordOnLogin)
        {
            UserLog.CredentialOutcomeComposed(_logger, userId, "MustChange");
            return GenericResult<ICredentialOutcome>.Success(CredentialOutcomes.ByName("MustChange"));
        }

        UserLog.CredentialOutcomeComposed(_logger, userId, "Match");
        return GenericResult<ICredentialOutcome>.Success(CredentialOutcomes.ByName("Match"));
    }

    // Why: on a wrong password, increment the non-secret failure counter and lock the account when it
    // reaches the configured threshold. The counter write is best-effort — its failure logs but never
    // grants access. Returns TooManyAttempts when the lock trips, otherwise the generic NoMatch.
    private async Task<IGenericResult<ICredentialOutcome>> OnNoMatch(
        Guid userId, UserConfiguration userCfg, PasswordPolicyOptions policy, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var newCount = userCfg.FailedLoginCount + 1;
        DateTimeOffset? lockoutEnd = userCfg.LockoutEnd;
        var locked = policy.MaxFailedLoginAttempts > 0 && newCount >= policy.MaxFailedLoginAttempts;
        if (locked)
            lockoutEnd = now.AddMinutes(policy.LockoutDurationMinutes);

        var write = await WriteLoginAttempt(userId, userCfg, newCount, lockoutEnd, cancellationToken).ConfigureAwait(false);
        if (!write.IsSuccess)
            UserLog.LockoutCounterUpdateFailed(_logger, userId);

        if (locked)
            UserLog.AccountLockedOut(_logger, userId);

        UserLog.AuthenticationDenied(_logger, userId);
        return GenericResult<ICredentialOutcome>.Success(CredentialOutcomes.ByName(locked ? "TooManyAttempts" : "NoMatch"));
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Store(
        Guid userId, string secretType, string plaintext, CancellationToken cancellationToken = default)
    {
        // Why: this edge stores only the password secret.
        if (!string.Equals(secretType, PasswordSecretType, StringComparison.OrdinalIgnoreCase))
            return GenericResult.Failure(UserLog.SecretTypeNotSupported(_logger, userId, secretType));

        var serviceResult = await ResolveCredentialService(userId, secretType, cancellationToken).ConfigureAwait(false);
        if (!serviceResult.IsSuccess || serviceResult.Value is null)
            return serviceResult;

        var algorithm = PasswordHashAlgorithms.ByName(_passwordPolicy.Value.PasswordHashAlgorithm);
        if (algorithm == PasswordHashAlgorithms.NotFound)
            return GenericResult.Failure(
                UserLog.VaultAlgorithmNotFound(_logger, userId, secretType, _passwordPolicy.Value.PasswordHashAlgorithm));

        // Why: generate a fresh salt and derive — the salt + algorithm are edge-owned; only the derived
        // hash (peppered inside the vault) is the secret. The plaintext is not used after this.
        var hashResult = algorithm.HashPassword(plaintext);

        var createResult = await serviceResult.Value
            .Create(userId, Convert.FromBase64String(hashResult.Hash), cancellationToken)
            .ConfigureAwait(false);
        if (!createResult.IsSuccess)
        {
            UserLog.VaultStoreFailed(_logger, userId, secretType);
            return createResult;
        }

        // Why: persist the edge-owned salt + algorithm + change timestamp and clear must-change.
        // Get the current user record, update only the security columns, then save back through the provider.
        var userResult = await _userProvider.GetUser(userId, cancellationToken).ConfigureAwait(false);
        if (!userResult.IsSuccess || userResult.Value is null)
        {
            UserLog.VaultStoreFailed(_logger, userId, secretType);
            return userResult.Messages.Any()
                ? (IGenericResult)userResult
                : GenericResult.Failure(UserLog.VaultStoreFailed(_logger, userId, secretType));
        }

        var cfg = userResult.Value;
        cfg.Salt = hashResult.Salt;
        cfg.AlgorithmName = hashResult.AlgorithmName;
        cfg.LastPasswordChangedAt = DateTimeOffset.UtcNow;
        cfg.MustChangePasswordOnLogin = false;

        var updateResult = await _userProvider.Save(cfg, cancellationToken).ConfigureAwait(false);
        if (!updateResult.IsSuccess)
        {
            UserLog.VaultStoreFailed(_logger, userId, secretType);
            return (IGenericResult)updateResult;
        }

        UserLog.VaultStored(_logger, userId, secretType, hashResult.AlgorithmName);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public async Task<IGenericResult> ForcePasswordChange(Guid userId, CancellationToken cancellationToken = default)
    {
        // Why: only the non-secret flag is set — the current credential stays valid so the user can log
        // in to change it (Verify returns MustChange, which denies normal access but signals the change).
        var userResult = await _userProvider.GetUser(userId, cancellationToken).ConfigureAwait(false);
        if (!userResult.IsSuccess || userResult.Value is null)
        {
            UserCredentialLog.ForceChangeFailed(_logger, userId);
            return userResult.Messages.Any()
                ? (IGenericResult)userResult
                : GenericResult.Failure(UserLog.UserQueryByIdFailed(_logger, new InvalidOperationException("User not found"), userId));
        }

        var cfg = userResult.Value;
        cfg.MustChangePasswordOnLogin = true;

        var result = await _userProvider.Save(cfg, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            UserCredentialLog.ForceChangeFailed(_logger, userId);
            return (IGenericResult)result;
        }

        UserCredentialLog.ForceChangeSet(_logger, userId);
        return GenericResult.Success();
    }

    private async Task<IGenericResult<ICredentialService>> ResolveCredentialService(
        Guid userId, string secretType, CancellationToken cancellationToken)
    {
        if (_credentialService is not null)
            return GenericResult<ICredentialService>.Success(_credentialService);

        var serviceName = _usersOptions.Value.CredentialServiceName;
        if (string.IsNullOrWhiteSpace(serviceName))
            return GenericResult<ICredentialService>.Failure(UserLog.CredentialServiceNameMissing(_logger));

        var serviceResult = await _credentialServiceProvider
            .Get(new CredentialServiceRequest(null, serviceName), cancellationToken)
            .ConfigureAwait(false);

        if (!serviceResult.IsSuccess || serviceResult.Value is null)
            return GenericResult<ICredentialService>.Failure(
                UserLog.CredentialServiceResolveFailed(_logger, serviceName!, userId, secretType));

        _credentialService = serviceResult.Value;
        return GenericResult<ICredentialService>.Success(_credentialService);
    }

    // Why: GetUserSecurity now reads the UserConfiguration record through the provider.
    // Security columns (Salt, AlgorithmName, FailedLoginCount, LockoutEnd, MustChangePasswordOnLogin,
    // LastPasswordChangedAt) are projected from the record — no separate projection needed.
    // Why: returns the provider result AS-IS (not collapsed to a nullable) so the caller can tell apart
    // "lookup succeeded, no matching user" (IsSuccess=true, Value=null — genuinely unknown account, decoy
    // path applies) from "the provider/gateway FAILED" (IsSuccess=false — an infrastructure error that
    // must fail loud, never silently treated as an unknown account).
    private Task<IGenericResult<UserConfiguration?>> GetUserSecurity(Guid userId, CancellationToken cancellationToken)
        => _userProvider.GetUser(userId, cancellationToken);

    private async Task<IGenericResult<int>> WriteLoginAttempt(
        Guid userId, UserConfiguration cfg, int failedCount, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        cfg.FailedLoginCount = failedCount;
        cfg.LockoutEnd = lockoutEnd;
        var saveResult = await _userProvider.Save(cfg, cancellationToken).ConfigureAwait(false);
        // Why: Save returns IGenericResult<UserConfiguration>; wrap as IGenericResult<int> so the
        // caller's signature (WriteLoginAttempt→IGenericResult<int>) stays consistent with prior behaviour.
        return saveResult.IsSuccess
            ? GenericResult<int>.Success(1)
            : saveResult.ToNewResult<int>();
    }

    private async Task ResetLockout(Guid userId, UserConfiguration cfg, CancellationToken cancellationToken)
    {
        var write = await WriteLoginAttempt(userId, cfg, 0, null, cancellationToken).ConfigureAwait(false);
        if (!write.IsSuccess)
            UserLog.LockoutCounterUpdateFailed(_logger, userId);
    }
}
