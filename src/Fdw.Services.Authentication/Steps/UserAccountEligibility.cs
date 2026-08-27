using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Configuration;
using Fdw.Services.Users.Commands;
using Fdw.Services.Users.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Steps;

/// <summary>
/// Decides whether a principal may hold a token, from the state of their account.
/// </summary>
/// <remarks>
/// Answers the login-time question only. Whether they may then perform some action on some object
/// is asked per request against a live decision point, thousands of times, and is never settled here.
/// </remarks>
public sealed class UserAccountEligibility : IIssuanceEligibility
{
    private readonly ImplementationConfigurationProviderBase<UserConfiguration, UserConfigurationCommand> _users;
    private readonly ILogger<UserAccountEligibility> _logger;

    /// <summary>Initializes a new instance of the <see cref="UserAccountEligibility"/> class.</summary>
    /// <param name="users">Reads user records.</param>
    /// <param name="logger">The logger.</param>
    public UserAccountEligibility(
        ImplementationConfigurationProviderBase<UserConfiguration, UserConfigurationCommand> users,
        ILogger<UserAccountEligibility>? logger = null)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _logger = logger ?? NullLogger<UserAccountEligibility>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<Decision>> MayBeIssued(
        Principal principal, CancellationToken cancellationToken = default)
    {
        if (principal is null)
            return GenericResult<Decision>.Failure(EligibilityLog.PrincipalMissing(_logger));

        var user = await _users.Get(principal.Id, cancellationToken).ConfigureAwait(false);

        // Why a failure and not a denial: the read failing means the question was not answered,
        // which is not the same as answering no — a caller retries one and not the other.
        if (user.IsFailure)
            return user.ToNewResult<Decision>();

        if (user.Value is null)
            return Deny(principal.Id, "the principal names no user");

        if (user.Value.IsDeleted)
            return Deny(principal.Id, "the account is deleted");

        if (!user.Value.IsActive)
            return Deny(principal.Id, "the account is disabled");

        if (user.Value.LockoutEnd is { } until && until > DateTimeOffset.UtcNow)
            return Deny(principal.Id, "the account is locked out");

        if (user.Value.TenantId != principal.TenantId)
            // Why refused: the principal was resolved with one tenant and the record says another.
            // Proceeding would issue a token asserting a tenancy the user does not have.
            return Deny(principal.Id, "the principal's tenant does not match the account's");

        EligibilityLog.Permitted(_logger, principal.Id);

        return GenericResult<Decision>.Success(
            new Decision { Permitted = true, Reason = "the account is active" });
    }

    private IGenericResult<Decision> Deny(Guid principalId, string reason)
    {
        EligibilityLog.Denied(_logger, principalId, reason);
        return GenericResult<Decision>.Success(new Decision { Permitted = false, Reason = reason });
    }
}
