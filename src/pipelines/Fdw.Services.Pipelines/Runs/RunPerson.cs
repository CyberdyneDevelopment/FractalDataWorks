using System;
using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Services.Pipelines.Runs;

/// <summary>
/// The person a caller is, for a pipeline run that acts as them (FDW-6137): the one rule by which <see cref="PipelineRuns"/> fills a run's person and a
/// host stamps the person on a request it keeps (a run held for approval), so the two never disagree.
/// </summary>
/// <remarks>
/// A caller is a person when it is authenticated, is not the system, is not a run acting as no person (<see cref="TenantRunAuthenticationContext"/>) and
/// names a user by a non-empty Guid: the rule by which the connection reads a caller's own rows (<c>MsSqlSessionContextBase.IsResolvedUser</c>), so the
/// run's person is exactly the caller the data would see. Public because a host outside FDW keeps requests too.
/// </remarks>
public static class RunPerson
{
    /// <summary>Gets the person a caller is.</summary>
    /// <param name="caller">The authenticated caller, or null when there is none.</param>
    /// <returns>The person's id, or null for a caller that is no person.</returns>
    public static Guid? Of(IAuthenticationContext? caller) =>
        caller is { IsAuthenticated: true, IsSystemContext: false } and not TenantRunAuthenticationContext
        && Guid.TryParse(caller.UserId, out var person) && person != Guid.Empty
            ? person
            : null;
}
