using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Etl.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Decides whether a caller may record the roles they named as the roles a stored trigger's runs act with: a schedule's (FDW-1161) or
/// a webhook trigger's (FDW-1884).
/// </summary>
/// <remarks>
/// <para>
/// A schedule fires, and a webhook is called, with no person behind it, so each holds the roles its creator named; every run acts
/// with exactly those roles. The creator may name only roles they hold in the tenant they act in (<see cref="RunRoleSelection"/>),
/// and every role must exist, so a trigger never carries more than its creator could do and never carries a role that cannot be
/// resolved.
/// </para>
/// <para>
/// What the roles can reach is judged when a run starts (<c>PipelineRunAuthority</c>), against the pipeline as it is stored
/// then, not here: the pipeline can change after the trigger is saved, and a run that the roles can no longer reach is refused
/// naming the resource.
/// </para>
/// </remarks>
public sealed class RunRoleAuthority
{
    private readonly IRoleGrants _grants;

    /// <summary>Initializes a new instance of the <see cref="RunRoleAuthority"/> class.</summary>
    /// <param name="grants">Confirms the roles exist.</param>
    public RunRoleAuthority(IRoleGrants grants)
    {
        _grants = grants ?? throw new System.ArgumentNullException(nameof(grants));
    }

    /// <summary>Decides whether <paramref name="caller"/> may record <paramref name="requested"/> as the roles of a schedule.</summary>
    /// <param name="caller">The caller creating or changing the schedule.</param>
    /// <param name="pipelineName">The pipeline the schedule runs, for the refusal to name.</param>
    /// <param name="requested">The roles the request names.</param>
    /// <param name="logger">The endpoint's logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The roles to record, or a 400-class refusal naming the role and why.</returns>
    public async Task<IGenericResult<IReadOnlyList<string>>> Authorize(
        IAuthenticationContext caller,
        string pipelineName,
        IEnumerable<string>? requested,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var chosen = RunRoleSelection.Chosen(requested);
        if (RunRoleSelection.Refusal(caller, chosen) is { } reason)
            return Refuse(logger, pipelineName, chosen, reason);

        var granted = await _grants.Get(chosen, cancellationToken).ConfigureAwait(false);
        return granted.IsFailure
            ? Refuse(logger, pipelineName, chosen, string.Concat(granted.CurrentMessage))
            : GenericResult<IReadOnlyList<string>>.Success(chosen);
    }

    private static IGenericResult<IReadOnlyList<string>> Refuse(
        ILogger logger, string pipelineName, IReadOnlyList<string> roles, string reason) =>
        GenericResult.Failure(EtlResultCodes.ByName("PipelineRunRefused"), logger,
            ResultDetails.Create("PipelineName", pipelineName, "RunRoles", roles.Count == 0 ? "no role" : string.Join(", ", roles), "Reason", reason))
            .ToNewResult<IReadOnlyList<string>>();
}
