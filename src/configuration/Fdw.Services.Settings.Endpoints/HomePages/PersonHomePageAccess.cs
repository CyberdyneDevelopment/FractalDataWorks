using System;
using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// The scope of the caller's own home page (FDW-2471). A person reaches only their own page, so it is taken from who is signed in and never from the
/// request, and it needs no right.
/// </summary>
internal static class PersonHomePageAccess
{
    /// <summary>Reads the caller's own scope.</summary>
    /// <returns>The scope, or null when no person is signed in or they act in no tenant.</returns>
    internal static HomePageScope? Of(IAuthenticationContextAccessor authContext)
        => authContext.Current is { IsAuthenticated: true, ActiveTenantId: { } tenantId } caller && Guid.TryParse(caller.UserId, out var userId)
            ? new HomePageScope(HomePageKinds.Person, userId, tenantId)
            : null;
}
