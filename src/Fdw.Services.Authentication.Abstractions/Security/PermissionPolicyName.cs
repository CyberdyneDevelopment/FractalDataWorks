using System.Collections.Generic;

namespace Fdw.Services.Authentication.Abstractions.Security;

/// <summary>
/// Reads the name of an endpoint policy as the permissions it asks for. The policy provider that builds the policy and the pre-processor that checks the token's
/// <c>perm</c> claims before the policy runs both read the name through this one rule, so they cannot disagree about what an endpoint requires.
/// </summary>
/// <remarks>
/// A name is <c>{resource}:{action}</c>, or several of those joined by a bar (<c>ui-feedback:read|ui-feedback:write</c>) when any one of them is enough. A name in
/// which any part is not <c>{resource}:{action}</c> is not a permission policy at all.
/// </remarks>
public static class PermissionPolicyName
{
    /// <summary>The character that joins permissions of which any one is enough.</summary>
    public const char Alternative = '|';

    /// <summary>Reads a policy name.</summary>
    /// <param name="policyName">The name an endpoint declared.</param>
    /// <param name="permissions">The permissions of which any one satisfies the policy, in the order named; empty when the name is not a permission policy.</param>
    /// <returns>True when every part of the name is <c>{resource}:{action}</c>.</returns>
    public static bool TryParse(string policyName, out IReadOnlyList<(string Resource, string Action)> permissions)
    {
        var parsed = new List<(string Resource, string Action)>();
        permissions = parsed;

        foreach (var part in policyName.Split(Alternative))
        {
            var colon = part.IndexOf(':');
            if (colon <= 0 || colon == part.Length - 1)
            {
                parsed.Clear();
                return false;
            }

            parsed.Add((part.Substring(0, colon), part.Substring(colon + 1)));
        }

        return true;
    }
}
