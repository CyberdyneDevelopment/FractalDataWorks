using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Claims.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Users.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Abstract base class for requiring a user to change their password at next login
/// (POST /users/{IdOrName}/require-password-change).
/// </summary>
/// <remarks>
/// <para>
/// The account belongs to its HOME tenant, so this is for that tenant's administrator — a caller
/// acting in the account's home tenant — or the platform administrator (FDW-825). An admin of some
/// other tenant the user also belongs to manages that membership and the roles held in it, never the
/// account itself. Row-level security enforces the same rule on the account row; checking it here
/// first turns a refused write into a clear 403 instead of a failed save.
/// </para>
/// <para>
/// The user is not locked out: their next sign-in is answered "password change required", and they
/// set a new password by proving the current one.
/// </para>
/// </remarks>
public abstract class RequirePasswordChangeEndpointBase : Endpoint<UserScopedRequest>
{
    private readonly UserConfigurationProvider _userProvider;
    private readonly IUserCredentialService _credentialService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequirePasswordChangeEndpointBase"/> class.
    /// </summary>
    /// <param name="userProvider">The user configuration provider.</param>
    /// <param name="credentialService">The user credential service.</param>
    /// <param name="logger">The logger.</param>
    protected RequirePasswordChangeEndpointBase(
        UserConfigurationProvider userProvider,
        IUserCredentialService credentialService,
        ILogger logger)
    {
        _userProvider = userProvider;
        _credentialService = credentialService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the policy name for this operation.
    /// </summary>
    protected virtual string WritePolicy => "users:write";

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/users/{IdOrName}/require-password-change");
        Policies(WritePolicy);
        Summary(s =>
        {
            s.Summary = "Require a user to change their password at next login";
            s.Description = "Marks the account so its next sign-in must set a new password. Allowed for the account's home-tenant administrator and the platform administrator.";
        });
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to add tags or further configuration.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc />
    public override async Task HandleAsync(UserScopedRequest req, CancellationToken ct)
    {
        var userResult = Guid.TryParse(req.IdOrName, out var requestedId)
            ? await _userProvider.Get(requestedId, ct).ConfigureAwait(false)
            : await _userProvider.Get(req.IdOrName, ct).ConfigureAwait(false);

        // Row-level security hides accounts outside the caller's tenant, so "not found" also covers
        // "not yours to see".
        if (!userResult.IsSuccess || userResult.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var user = userResult.Value;

        var actingIn = User.FindFirst(ClaimDefinitions.tenantId.Name)?.Value;
        var isHomeTenant = Guid.TryParse(actingIn, out var actingTenantId) && user.TenantId == actingTenantId;
        if (!isHomeTenant && !Resolve<ISystemRoleConfiguration>().IsPlatformAdmin(User))
        {
            UserEndpointLog.RequirePasswordChangeNotHomeTenant(_logger, req.IdOrName);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        var result = await _credentialService.ForcePasswordChange(user.Id, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            AddError("The account could not be marked to change its password.");
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        UserEndpointLog.PasswordChangeRequired(_logger, req.IdOrName);
        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
