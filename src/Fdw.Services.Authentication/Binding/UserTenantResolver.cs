using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Configuration;
using Fdw.Services.Users.Configuration;
using Fdw.Services.Users.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Binding;

/// <summary>
/// Reads the tenant a user belongs to from the user record.
/// </summary>
public sealed class UserTenantResolver : ITenantResolver
{
    private readonly ImplementationConfigurationProviderBase<UserConfiguration, UserConfigurationCommand> _users;
    private readonly ILogger<UserTenantResolver> _logger;

    /// <summary>Initializes a new instance of the <see cref="UserTenantResolver"/> class.</summary>
    /// <param name="users">Reads user records.</param>
    /// <param name="logger">The logger.</param>
    public UserTenantResolver(
        ImplementationConfigurationProviderBase<UserConfiguration, UserConfigurationCommand> users,
        ILogger<UserTenantResolver>? logger = null)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _logger = logger ?? NullLogger<UserTenantResolver>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<Guid>> TenantFor(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.Get(userId, cancellationToken).ConfigureAwait(false);
        if (user.IsFailure)
            return user.ToNewResult<Guid>();

        // Why a failure and not a default tenant: a user whose tenant is unknown would otherwise be
        // authenticated into whichever one the default named, and that is a cross-tenant leak with
        // a plausible-looking cause.
        return user.Value?.TenantId is { } tenantId && tenantId != Guid.Empty
            ? GenericResult<Guid>.Success(tenantId)
            : GenericResult<Guid>.Failure(BindingLog.TenantUnknown(_logger, userId));
    }
}
