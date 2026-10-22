using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Services.Authentication.Sessions;

/// <summary>
/// The rule for acting with a role (FDW-2282): the grant of the role acted with must be no wider than the grant of the session acted from.
/// </summary>
/// <remarks>
/// Whether a permission is held is answered by <see cref="PermissionCoverage"/>, the one rule every other check uses, so acting cannot be allowed
/// something a request would be refused. This asks "does the caller hold what the role carries", not "may the caller give the role away", which
/// is the grant rule's question and also demands the delegate right.
/// </remarks>
public static class ActingGrant
{
    /// <summary>The permissions of the role that the caller's own grant does not cover.</summary>
    /// <param name="held">The permissions the acting-from session holds (its <c>perm</c> claims).</param>
    /// <param name="roleGrants">The permissions the role carries.</param>
    /// <returns>The permissions to refuse for, each once; empty when the role is no wider than the caller.</returns>
    public static IReadOnlyList<string> NotCovered(IEnumerable<string> held, IEnumerable<string> roleGrants)
    {
        ArgumentNullException.ThrowIfNull(held);
        ArgumentNullException.ThrowIfNull(roleGrants);

        var heldList = held.ToList();
        return [.. roleGrants.Where(permission => !PermissionCoverage.Covers(heldList, permission)).Distinct(StringComparer.Ordinal)];
    }
}
