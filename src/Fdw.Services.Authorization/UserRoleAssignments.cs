using System;
using Fdw.Services.Authorization.Configuration;

namespace Fdw.Services.Authorization;

/// <summary>Which UserRole rows belong to a user.</summary>
/// <remarks>
/// The provider does not own this comparison -- a caller states its predicate through
/// <c>Find</c> -- so it lives here, once, where every caller that asks "what does this user hold"
/// reaches it. Written at each call site it had already drifted: one site skipped a row that is not
/// a user's and stayed silent, the other logged a warning for it (FDW-991).
/// </remarks>
public static class UserRoleAssignments
{
    /// <summary>The predicate that selects the assignments held by one user.</summary>
    /// <param name="userId">The user's id.</param>
    /// <returns>A predicate for <c>Find</c>: true for a row whose UserId is that user's Guid, in any spelling.</returns>
    // Why parsed Guids and no log for the rest: UserId is a subject held as text (VARCHAR). A user's
    // subject is their Guid, and SQL Server's CAST(uniqueidentifier AS VARCHAR) is uppercase while
    // Guid.ToString() is lowercase, so text compared as text never matches (FDW-532). A service
    // identity's subject is the name it was declared with ('fdw.scheduler'), which is not a Guid and
    // is not meant to be one: it is another identity, so it does not match a user, and there is
    // nothing malformed to report about it.
    public static Func<IUserRoleImplementationConfiguration, bool> HeldBy(Guid userId)
        => assignment => Guid.TryParse(assignment.UserId, out var holder) && holder == userId;
}
