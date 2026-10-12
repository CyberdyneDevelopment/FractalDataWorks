using System;
using System.Collections.Generic;

namespace Fdw.Services.Authentication.Abstractions.Security;

/// <summary>
/// Decides whether a set of held permissions covers a required one. Every check of a <c>perm</c> claim
/// answers through this one rule, so an endpoint policy, a handler and a service never disagree.
/// </summary>
/// <remarks>
/// <para>
/// A permission is <c>{resource}:{action}</c>. A required permission is covered by:
/// <list type="bullet">
///   <item><description>itself;</description></item>
///   <item><description><c>{resource}:*</c>, every action on the resource.</description></item>
/// </list>
/// There is no grant of everything: <c>*:*</c> covers nothing. No seed or endpoint creates it, and a token
/// carries the permissions its roles name, so a super-grant could only arrive through a direct write to the
/// catalogue -- exactly the row a check must not trust.
/// </para>
/// <para>
/// A per-resource permission names one resource by id: <c>{resource}/{id}:{action}</c>, where the
/// last path segment of the resource is a <see cref="Guid"/>. It is also covered by the
/// resource-wide forms <c>{resource}:{action}</c> and <c>{resource}:*</c>, because a grant over every
/// item of a resource includes this one. A resource whose last segment is not a Guid
/// (<c>settings/role</c>, <c>quality/rules</c>) is a resource name in its own right and is never
/// widened to its first segment.
/// </para>
/// </remarks>
public static class PermissionCoverage
{
    /// <summary>Returns whether <paramref name="held"/> covers <paramref name="required"/>.</summary>
    /// <param name="held">The permissions the caller carries.</param>
    /// <param name="required">The permission asked for, <c>{resource}:{action}</c>.</param>
    /// <returns>True when any held permission covers the required one.</returns>
    public static bool Covers(IEnumerable<string> held, string required)
    {
        if (held is null) throw new ArgumentNullException(nameof(held));
        if (string.IsNullOrWhiteSpace(required)) throw new ArgumentException("A required permission is named.", nameof(required));

        var accepted = Accepting(required);
        foreach (var permission in held)
        {
            if (permission is null) continue;
            foreach (var candidate in accepted)
            {
                if (string.Equals(permission, candidate, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    /// <summary>The held permissions that would cover <paramref name="required"/>.</summary>
    /// <param name="required">The permission asked for.</param>
    /// <returns>Every permission name that covers it.</returns>
    public static IReadOnlyList<string> Accepting(string required)
    {
        if (string.IsNullOrWhiteSpace(required)) throw new ArgumentException("A required permission is named.", nameof(required));

        var accepted = new List<string> { required };
        var colon = required.LastIndexOf(':');
        if (colon <= 0 || colon == required.Length - 1)
            return accepted;

        var resource = required.Substring(0, colon);
        var action = required.Substring(colon + 1);
        accepted.Add(resource + ":*");

        var slash = resource.LastIndexOf('/');
        if (slash > 0 && Guid.TryParse(resource.Substring(slash + 1), out _))
        {
            var whole = resource.Substring(0, slash);
            accepted.Add(whole + ":" + action);
            accepted.Add(whole + ":*");
        }

        return accepted;
    }
}
