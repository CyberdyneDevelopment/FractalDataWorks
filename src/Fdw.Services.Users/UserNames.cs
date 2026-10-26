using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Users.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Users;

/// <summary>
/// Reads a person's user name from their own account, through the user provider (FDW-2199).
/// </summary>
/// <remarks>
/// The name of a configured member is the name of its domain row, and the provider stamps it on the member it returns, so the read is the same one
/// <c>GET /auth/me</c> makes. The caller is a member of the tenant they act in, so the read needs no elevation.
/// </remarks>
public sealed class UserNames : IUserNames
{
    private readonly IUserConfigurationProvider _users;
    private readonly ILogger<UserNames> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserNames"/> class.
    /// </summary>
    /// <param name="users">The provider the user's account is read through.</param>
    /// <param name="logger">The logger.</param>
    public UserNames(IUserConfigurationProvider users, ILogger<UserNames>? logger)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _logger = logger ?? NullLogger<UserNames>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<string>> Get(Guid userId, CancellationToken cancellationToken = default)
    {
        UserNamesLog.Reading(_logger, userId);

        var found = await _users.Get(userId, cancellationToken).ConfigureAwait(false);
        if (!found.IsSuccess)
            return found.ToNewResult<string>();

        if (found.Value is not { } account)
            return GenericResult<string>.Failure(UserNamesLog.AccountNotFound(_logger, userId));

        return string.IsNullOrWhiteSpace(account.Name)
            ? GenericResult<string>.Failure(UserNamesLog.NameBlank(_logger, userId))
            : GenericResult<string>.Success(account.Name);
    }
}
