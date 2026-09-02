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

    private const string DecoySaltBase64 = "ZmR3LWRlY295LXNhbHQwMQ==";

    // The users domain's own configuration row, on the domain's own store.
    private const string ConfigurationName = "UsersService";

    private readonly ICredentialServiceProvider _credentialServiceProvider;
    private readonly UsersServiceConfigurationProvider _configuration;
    private readonly UserConfigurationProvider _userProvider;
    private readonly ILogger<UserCredentialService> _logger;

    private ICredentialService? _credentialService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserCredentialService"/> class.
    /// </summary>
    public UserCredentialService(
        ICredentialServiceProvider credentialServiceProvider,
        UsersServiceConfigurationProvider configuration,
        UserConfigurationProvider userProvider,
        ILogger<UserCredentialService>? logger = null)
    {
        _credentialServiceProvider = credentialServiceProvider ?? throw new ArgumentNullException(nameof(credentialServiceProvider));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _userProvider = userProvider ?? throw new ArgumentNullException(nameof(userProvider));
        _logger = logger ?? NullLogger<UserCredentialService>.Instance;
    }

    /// <inheritdoc />
#pragma warning disable MA0051 // Why: sequential fail-loud + uniform-timing branches read top-to-bottom; splitting hurts the anti-enumeration flow.
    public async Task<IGenericResult<ICredentialOutcome>> Verify(
        Guid userId, string secretType, string plaintext, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(secretType, PasswordSecretType, StringComparison.OrdinalIgnoreCase))
            return GenericResult<ICredentialOutcome>.Failure(UserLog.SecretTypeNotSupported(_logger, userId, secretType));

        var configurationResult = await LoadConfiguration(cancellationToken).ConfigureAwait(false);
        if (configurationResult.IsFailure)
            return configurationResult.ToNewResult<ICredentialOutcome>();

        var policy = configurationResult.Value!;
        if (policy.MaxFailedLoginAttempts > 0 && policy.LockoutDurationMinutes <= 0)
            return GenericResult<ICredentialOutcome>.Failure(
                UserLog.PasswordPolicyInvalid(_logger, "MaxFailedLoginAttempts is set but LockoutDurationMinutes is not positive"));

        var serviceResult = await ResolveCredentialService(userId, secretType, cancellationToken).ConfigureAwait(false);
        if (!serviceResult.IsSuccess || serviceResult.Value is null)
            return serviceResult.ToNewResult<ICredentialOutcome>();

        var userCfgResult = await GetUserSecurity(userId, cancellationToken).ConfigureAwait(false);

        if (!userCfgResult.IsSuccess)
            return GenericResult<ICredentialOutcome>.Failure(UserLog.UserSecurityLookupFailed(_logger, userId));

        var userCfg = userCfgResult.Value;

        if (userCfg is null || string.IsNullOrEmpty(userCfg.Salt) || string.IsNullOrWhiteSpace(userCfg.AlgorithmName))
            return RunDecoyAndDeny(userId, plaintext, policy);

        var algorithm = PasswordHashAlgorithms.ByName(userCfg.AlgorithmName);
        if (algorithm == PasswordHashAlgorithms.NotFound)
            return GenericResult<ICredentialOutcome>.Failure(
                UserLog.VaultAlgorithmNotFound(_logger, userId, secretType, userCfg.AlgorithmName));

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

    private IGenericResult<ICredentialOutcome> RunDecoyAndDeny(Guid userId, string plaintext, UsersServiceConfiguration policy)
    {
        var decoyAlgorithm = PasswordHashAlgorithms.ByName(policy.PasswordHashAlgorithm);
        if (decoyAlgorithm != PasswordHashAlgorithms.NotFound)
            _ = decoyAlgorithm.DeriveKey(plaintext, DecoySaltBase64);

        UserLog.NoPasswordOnFileDecoy(_logger, userId);
        UserLog.AuthenticationDenied(_logger, userId);
        return GenericResult<ICredentialOutcome>.Success(CredentialOutcomes.ByName("NoMatch"));
    }

    private async Task<IGenericResult<ICredentialOutcome>> ComposeSuccessOutcome(
        Guid userId, UserConfiguration userCfg, UsersServiceConfiguration policy, DateTimeOffset now, CancellationToken cancellationToken)
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

    private async Task<IGenericResult<ICredentialOutcome>> OnNoMatch(
        Guid userId, UserConfiguration userCfg, UsersServiceConfiguration policy, DateTimeOffset now, CancellationToken cancellationToken)
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
        if (!string.Equals(secretType, PasswordSecretType, StringComparison.OrdinalIgnoreCase))
            return GenericResult.Failure(UserLog.SecretTypeNotSupported(_logger, userId, secretType));

        var serviceResult = await ResolveCredentialService(userId, secretType, cancellationToken).ConfigureAwait(false);
        if (!serviceResult.IsSuccess || serviceResult.Value is null)
            return serviceResult;

        var configurationResult = await LoadConfiguration(cancellationToken).ConfigureAwait(false);
        if (configurationResult.IsFailure)
            return configurationResult;

        var algorithm = PasswordHashAlgorithms.ByName(configurationResult.Value!.PasswordHashAlgorithm);
        if (algorithm == PasswordHashAlgorithms.NotFound)
            return GenericResult.Failure(
                UserLog.VaultAlgorithmNotFound(_logger, userId, secretType, configurationResult.Value!.PasswordHashAlgorithm));

        var hashResult = algorithm.HashPassword(plaintext);

        var createResult = await serviceResult.Value
            .Create(userId, Convert.FromBase64String(hashResult.Hash), cancellationToken)
            .ConfigureAwait(false);
        if (!createResult.IsSuccess)
        {
            UserLog.VaultStoreFailed(_logger, userId, secretType);
            return createResult;
        }

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

    // Why a helper: every caller needs the row and a non-null value, and folding the two checks
    // into one keeps each call site to a single branch.
    private async Task<IGenericResult<UsersServiceConfiguration>> LoadConfiguration(CancellationToken cancellationToken)
    {
        var result = await _configuration.Get(ConfigurationName, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return result;

        return result.Value is null
            ? GenericResult<UsersServiceConfiguration>.Failure(
                UserLog.CredentialServiceNameMissing(_logger))
            : result;
    }

    private async Task<IGenericResult<ICredentialService>> ResolveCredentialService(
        Guid userId, string secretType, CancellationToken cancellationToken)
    {
        if (_credentialService is not null)
            return GenericResult<ICredentialService>.Success(_credentialService);

        var configurationResult = await LoadConfiguration(cancellationToken).ConfigureAwait(false);
        if (configurationResult.IsFailure)
            return configurationResult.ToNewResult<ICredentialService>();

        var serviceName = configurationResult.Value!.CredentialServiceName;
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

    private Task<IGenericResult<UserConfiguration?>> GetUserSecurity(Guid userId, CancellationToken cancellationToken)
        => _userProvider.GetUser(userId, cancellationToken);

    private async Task<IGenericResult<int>> WriteLoginAttempt(
        Guid userId, UserConfiguration cfg, int failedCount, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        cfg.FailedLoginCount = failedCount;
        cfg.LockoutEnd = lockoutEnd;
        var saveResult = await _userProvider.Save(cfg, cancellationToken).ConfigureAwait(false);
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
