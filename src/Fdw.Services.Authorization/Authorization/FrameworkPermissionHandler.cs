using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Logging;

namespace Fdw.Services.Authorization.Authorization;

/// <summary>
/// ASP.NET Core authorization handler that bridges to the FDW
/// <see cref="IFrameworkAuthorizationService"/>. Converts ClaimsPrincipal
/// to <see cref="Fdw.Services.Authentication.Abstractions.Security.IAuthenticationContext"/>
/// and delegates permission checks to the database-backed authorization service.
/// </summary>
public sealed class FrameworkPermissionHandler : AuthorizationHandler<FdwPermissionRequirement>
{
    private readonly IFrameworkAuthorizationService _fdwAuthorizationService;
    private readonly ILogger<FrameworkPermissionHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameworkPermissionHandler"/> class.
    /// </summary>
    /// <param name="fdwAuthorizationService">The FDW authorization service.</param>
    /// <param name="logger">The logger.</param>
    public FrameworkPermissionHandler(
        IFrameworkAuthorizationService fdwAuthorizationService,
        ILogger<FrameworkPermissionHandler> logger)
    {
        _fdwAuthorizationService = fdwAuthorizationService ?? throw new ArgumentNullException(nameof(fdwAuthorizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        FdwPermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            FrameworkAuthorizationLog.RequirementSkippedUnauthenticated(
                _logger, requirement.Resource, requirement.Action);
            return;
        }

        // Why every accepted permission is asked, in order: a requirement written as "a:x|b:y" is met by either, so the first that is held
        // succeeds it; a check that errors is logged and the next is still asked, so one failing lookup cannot hide a permission that is held.
        var authContext = new ClaimsPrincipalAuthenticationContext(context.User);
        foreach (var (resource, action) in Accepted(requirement))
        {
            FrameworkAuthorizationLog.CheckingRequirement(_logger, resource, action);

            var result = await _fdwAuthorizationService.Authorize(authContext, resource, action).ConfigureAwait(false);
            if (result.IsFailure)
            {
                FrameworkAuthorizationLog.RequirementCheckFailed(_logger, resource, action, result.CurrentMessage);
                continue;
            }

            if (result.Value)
            {
                FrameworkAuthorizationLog.RequirementSatisfied(_logger, resource, action);
                context.Succeed(requirement);
                return;
            }
        }
    }

    private static IEnumerable<(string Resource, string Action)> Accepted(FdwPermissionRequirement requirement)
    {
        yield return (requirement.Resource, requirement.Action);
        foreach (var other in requirement.AlsoAccepted)
            yield return other;
    }
}
