using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Users.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Binding;

/// <summary>
/// Reads the tenant a login lands in: the user's DEFAULT membership.
/// </summary>
/// <remarks>
/// <para>
/// Not the account's home tenant (<c>usr.Users.TenantId</c>). The home tenant owns the account — its
/// admin manages it — while the landing tenant is the user's own preference among the tenants they
/// belong to, set by marking one membership default. The two start out the same and are allowed to
/// differ (FDW-825).
/// </para>
/// <para>
/// A user with no default membership has nowhere to land, and is refused rather than placed in one
/// of their tenants by guesswork.
/// </para>
/// </remarks>
public sealed class UserTenantResolver : ITenantResolver
{
    private readonly IUserTenantsHeld _memberships;
    private readonly IAuthenticationContextAccessor _authContextAccessor;
    private readonly ILogger<UserTenantResolver> _logger;

    /// <summary>Initializes a new instance of the <see cref="UserTenantResolver"/> class.</summary>
    /// <param name="memberships">Reads the tenants one person is a member of, by the person's id.</param>
    /// <param name="authContextAccessor">Establishes the trusted context for pre-authentication reads.</param>
    /// <param name="logger">The logger.</param>
    public UserTenantResolver(
        IUserTenantsHeld memberships,
        IAuthenticationContextAccessor authContextAccessor,
        ILogger<UserTenantResolver>? logger = null)
    {
        _memberships = memberships ?? throw new ArgumentNullException(nameof(memberships));
        _authContextAccessor = authContextAccessor ?? throw new ArgumentNullException(nameof(authContextAccessor));
        _logger = logger ?? NullLogger<UserTenantResolver>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<Guid>> TenantFor(Guid userId, CancellationToken cancellationToken = default)
    {
        // This platform authentication lookup precedes the request authentication context.
        using var systemScope = new SystemAuthenticationContextScope(_authContextAccessor);

        // The person's own memberships are read by the person's id, so the cost of a sign-in does not grow with the number of users (FDW-2714).
        var memberships = await _memberships.Get(userId, cancellationToken).ConfigureAwait(false);
        if (memberships.IsFailure)
            return memberships.ToNewResult<Guid>();

        // A filtered unique index allows one current default per user, so this is zero or one.
        return memberships.Value!.Where(m => m.IsDefault).ToList() is [var membership] && membership.TenantId != Guid.Empty
            ? GenericResult<Guid>.Success(membership.TenantId)
            : GenericResult<Guid>.Failure(BindingLog.TenantUnknown(_logger, userId));
    }
}
