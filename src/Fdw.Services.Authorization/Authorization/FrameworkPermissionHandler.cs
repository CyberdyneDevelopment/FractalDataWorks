using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Fdw.Services.Authentication.Abstractions.Security;

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
            return;
        }

        // Adapt ClaimsPrincipal to IAuthenticationContext
        var authContext = new ClaimsPrincipalAuthenticationContext(context.User);

        var result = await _fdwAuthorizationService.Authorize(
            authContext,
            requirement.Resource,
            requirement.Action).ConfigureAwait(false);

        if (result.IsSuccess && result.Value)
        {
            context.Succeed(requirement);
        }
    }
}
