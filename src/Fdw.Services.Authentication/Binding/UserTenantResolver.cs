using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;
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
    private readonly IUserTenantConfigurationProvider _memberships;
    private readonly IAuthenticationContextAccessor _authContextAccessor;
    private readonly ILogger<UserTenantResolver> _logger;

    /// <summary>Initializes a new instance of the <see cref="UserTenantResolver"/> class.</summary>
    /// <param name="memberships">Reads tenant memberships.</param>
    /// <param name="authContextAccessor">Establishes the trusted context for pre-authentication reads.</param>
    /// <param name="logger">The logger.</param>
    public UserTenantResolver(
        IUserTenantConfigurationProvider memberships,
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

        var defaults = await _memberships
            .Find<IUserTenantImplementationConfiguration>(m => m.UserId == userId && m.IsDefault, cancellationToken)
            .ConfigureAwait(false);
        if (defaults.IsFailure)
            return defaults.ToNewResult<Guid>();

        // A filtered unique index allows one current default per user, so this is zero or one.
        return defaults.Value is [var membership] && membership.TenantId != Guid.Empty
            ? GenericResult<Guid>.Success(membership.TenantId)
            : GenericResult<Guid>.Failure(BindingLog.TenantUnknown(_logger, userId));
    }
}
