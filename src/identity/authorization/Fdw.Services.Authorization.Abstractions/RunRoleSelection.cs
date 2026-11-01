using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// The rule for naming the roles a pipeline run acts with (FDW-1161), shared by everything that names them: a manual trigger and
/// a schedule's creator.
/// </summary>
/// <remarks>
/// Why the roles are named by the caller and checked against what the caller holds: a run acts with its roles' permissions, so
/// being allowed to name a role the caller does not hold would let anyone start a run with more than they may do themselves. A
/// request names at least one role; a blank name is not a name.
/// </remarks>
public static class RunRoleSelection
{
    /// <summary>Reads the roles a request names as the roles a run acts with: the names that are not blank, each once.</summary>
    /// <param name="roles">The request's <c>runRoles</c>; null when it states none.</param>
    /// <returns>The roles named; empty when none are.</returns>
    public static IReadOnlyList<string> Chosen(IEnumerable<string>? roles) =>
        roles is null ? [] : [.. roles.Where(role => !string.IsNullOrWhiteSpace(role)).Distinct(StringComparer.OrdinalIgnoreCase)];

    /// <summary>Says why the caller may not name these roles, or that it may.</summary>
    /// <param name="caller">The caller naming the roles.</param>
    /// <param name="chosen">The roles named, as <see cref="Chosen"/> read them.</param>
    /// <returns>The reason the roles are refused; null when the caller may name them.</returns>
    public static string? Refusal(IAuthenticationContext caller, IReadOnlyList<string> chosen)
    {
        if (chosen.Count == 0)
            return "a run names at least one role to act with";

        if (caller.ActiveTenantId is null)
            return "the caller acts in no tenant, so there is no tenant for the run to act in";

        var notHeld = chosen.FirstOrDefault(role => !caller.Roles.Contains(role, StringComparer.OrdinalIgnoreCase));
        return notHeld is null
            ? null
            : $"the caller does not hold role '{notHeld}' in this tenant, and may name only roles they hold";
    }
}
