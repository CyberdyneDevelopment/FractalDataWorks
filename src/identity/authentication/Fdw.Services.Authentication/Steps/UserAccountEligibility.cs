using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Users;
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
    private readonly IUserConfigurationProvider _users;
    private readonly IAuthenticationContextAccessor _authContextAccessor;
    private readonly ILogger<UserAccountEligibility> _logger;

    /// <summary>Initializes a new instance of the <see cref="UserAccountEligibility"/> class.</summary>
    /// <param name="users">Reads user records.</param>
    /// <param name="authContextAccessor">Establishes the trusted context for pre-authentication reads.</param>
    /// <param name="logger">The logger.</param>
    public UserAccountEligibility(
        IUserConfigurationProvider users,
        IAuthenticationContextAccessor authContextAccessor,
        ILogger<UserAccountEligibility>? logger = null)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _authContextAccessor = authContextAccessor ?? throw new ArgumentNullException(nameof(authContextAccessor));
        _logger = logger ?? NullLogger<UserAccountEligibility>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<Decision>> MayBeIssued(
        Principal principal, ITenantProvider tenants, CancellationToken cancellationToken = default)
    {
        if (principal is null)
            return GenericResult<Decision>.Failure(EligibilityLog.PrincipalMissing(_logger));

        // This platform authentication lookup precedes the request authentication context.
        using var systemScope = new SystemAuthenticationContextScope(_authContextAccessor);

        var user = await _users.Get(principal.Id, cancellationToken).ConfigureAwait(false);

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

        // Not "is this the account's home tenant": a user acts in any tenant they are a member of,
        // and a member of the global tenant may act in any tenant (FDW-825). One question, one
        // answer -- the tenant switch asks the same one before issuing.
        var access = await tenants
            .ValidateTenantAccess(principal.TenantId, principal.Id.ToString(), cancellationToken)
            .ConfigureAwait(false);
        if (access.IsFailure)
            return access.ToNewResult<Decision>();

        if (!access.Value)
            return Deny(principal.Id, "the principal may not act in the tenant the token would be issued for");

        // A token for a department is issued only to a person who may act in it (FDW-1802). The tenant provider answers, as it does for the
        // tenant: it is handed to every run, so this check holds nothing it cannot be built with at start-up. No department stated is the
        // ordinary token.
        if (principal.OrganizationId is { } organizationId
            && await RefusalForDepartment(principal, organizationId, tenants, cancellationToken).ConfigureAwait(false) is { } refusal)
            return refusal;

        EligibilityLog.Permitted(_logger, principal.Id);

        return GenericResult<Decision>.Success(
            new Decision { Permitted = true, Reason = "the account is active" });
    }

    // Why a person may not be issued a token for a department, or null when they may.
    private async Task<IGenericResult<Decision>?> RefusalForDepartment(
        Principal principal, Guid organizationId, ITenantProvider tenants, CancellationToken cancellationToken)
    {
        var access = await tenants
            .ValidateOrganizationAccess(principal.TenantId, organizationId, principal.Id.ToString(), cancellationToken)
            .ConfigureAwait(false);
        if (access.IsFailure)
            return access.ToNewResult<Decision>();

        return access.Value ? null : Deny(principal.Id, "the principal may not act in the department");
    }

    private IGenericResult<Decision> Deny(Guid principalId, string reason)
    {
        EligibilityLog.Denied(_logger, principalId, reason);
        return GenericResult<Decision>.Success(new Decision { Permitted = false, Reason = reason });
    }
}
