using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Users;
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
    private readonly IUserConfigurationProvider _users;
    private readonly IAuthenticationContextAccessor _authContextAccessor;
    private readonly ILogger<UserTenantResolver> _logger;

    /// <summary>Initializes a new instance of the <see cref="UserTenantResolver"/> class.</summary>
    /// <param name="users">Reads user records.</param>
    /// <param name="authContextAccessor">Establishes the trusted context for pre-authentication reads.</param>
    /// <param name="logger">The logger.</param>
    public UserTenantResolver(
        IUserConfigurationProvider users,
        IAuthenticationContextAccessor authContextAccessor,
        ILogger<UserTenantResolver>? logger = null)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _authContextAccessor = authContextAccessor ?? throw new ArgumentNullException(nameof(authContextAccessor));
        _logger = logger ?? NullLogger<UserTenantResolver>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<Guid>> TenantFor(Guid userId, CancellationToken cancellationToken = default)
    {
        // This platform authentication lookup precedes the request authentication context.
        using var systemScope = new SystemAuthenticationContextScope(_authContextAccessor);

        var user = await _users.Get(userId, cancellationToken).ConfigureAwait(false);
        if (user.IsFailure)
            return user.ToNewResult<Guid>();

        return user.Value?.TenantId is { } tenantId && tenantId != Guid.Empty
            ? GenericResult<Guid>.Success(tenantId)
            : GenericResult<Guid>.Failure(BindingLog.TenantUnknown(_logger, userId));
    }
}
