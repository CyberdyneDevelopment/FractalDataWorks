using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Claims.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Principal = Fdw.Services.Authentication.Abstractions.Context.Principal;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// The context a run's work acts in when a person started it (FDW-6137): that person, in the run's tenant, holding what both the person and the run's
/// roles hold, and never more.
/// </summary>
/// <remarks>
/// <para>
/// The person is judged now, not when the run was accepted. Their account must still be one a token may be issued to in the run's tenant
/// (<see cref="IIssuanceEligibility"/>, the check sign-in uses: not deleted, not disabled, not locked out, a member of the tenant), because
/// <see cref="IUserPermissions.Get"/> answers a disabled person with their full grants. Their permissions are then read live, and every permission the
/// run's roles carry (<see cref="IRoleGrants.Get"/>) must be covered by the person's own (<see cref="PermissionCoverage"/>, the rule <c>ActingGrant</c>
/// applies to a session acting with a role). A person who fails any of these is refused, never run as nobody.
/// </para>
/// <para>
/// The context is a <see cref="ClaimsPrincipalAuthenticationContext"/> over the claims an acting credential carries (<c>sub</c>, the tenant,
/// <c>globalTenant</c> for the global tenant, the run's roles and their permissions), so the connection reads the person's own rows
/// (<c>ForUserSessionContext</c>, with <c>security.fn_TenantFilter</c> checking the person's live membership) and every permission check sees the
/// person. No token is minted.
/// </para>
/// </remarks>
internal static class PipelineRunPerson
{
    private const string AuthenticationType = "PipelineRunPerson";

    /// <summary>
    /// Names each service the judge needs that a host which runs pipelines has not registered, one message each (FDW-6528): the account eligibility check, the tenant
    /// provider, the person's permissions and the role grants. Asked of the registrations, not of the services, so it depends on neither what resolves first nor on the order the
    /// domains registered in, and it is read when the host initializes, after every domain has registered.
    /// </summary>
    /// <remarks>
    /// Only a host that runs pipelines is asked, the test the run services use (<c>PipelineTypes.OperationalConnection</c> is named): the scheduler and the API start runs on the
    /// ETL host and never judge a person here. The per-run <see cref="RunPersonLog.RunPersonCannotBeJudged"/> remains the backstop for a host that skips the check.
    /// </remarks>
    /// <param name="services">The host's services.</param>
    /// <param name="logger">The logger the messages are recorded with.</param>
    /// <returns>One message per service not registered; none for a host that does not run pipelines.</returns>
    public static IEnumerable<Fdw.Messages.IGenericMessage> JudgeServiceGaps(IServiceProvider services, ILogger logger)
    {
        if (Fdw.Services.Pipelines.PipelineTypes.OperationalConnection is not { Length: > 0 })
            return [];

        var registered = services.GetRequiredService<IServiceProviderIsService>();
        return [.. new[] { typeof(IIssuanceEligibility), typeof(ITenantProvider), typeof(IUserPermissions), typeof(IRoleGrants) }
            .Where(service => !registered.IsService(service))
            .Select(service => RunPersonLog.JudgeServiceNotRegistered(logger, service.Name))];
    }

    /// <summary>Builds the person's context for a run.</summary>
    /// <param name="services">The run's services: the eligibility check, the tenant provider, the person's permissions and the role grants.</param>
    /// <param name="request">The run; it names its person, tenant and roles.</param>
    /// <param name="personId">The person the run acts as.</param>
    /// <param name="tenantId">The run's tenant.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">Cancels the reads.</param>
    /// <returns>The person's context, or the refusal naming why the person may not act.</returns>
    public static async Task<IGenericResult<IAuthenticationContext>> Resolve(
        IServiceProvider services, PipelineExecutionRequest request, Guid personId, Guid tenantId, ILogger logger, CancellationToken cancellationToken)
    {
        var carried = await Judge(services, new RunPersonSubject(request.ExecutionId, request.PipelineName, request.RunRoles), personId, tenantId, logger, cancellationToken).ConfigureAwait(false);
        if (carried.IsFailure || carried.Value is not { } permissions)
            return carried.ToNewResult<IAuthenticationContext>();

        var tenant = await services.GetRequiredService<ITenantProvider>().GetTenant(tenantId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure || tenant.Value is not { } read)
        {
            return GenericResult<IAuthenticationContext>.Failure(RunPersonLog.RunPersonTenantUnreadable(
                logger, request.ExecutionId, request.PipelineName, personId, tenantId, tenant.CurrentMessage));
        }

        RunPersonLog.RunActsAsPerson(logger, request.ExecutionId, request.PipelineName, personId, tenantId, string.Join(", ", request.RunRoles));
        return GenericResult<IAuthenticationContext>.Success(new ClaimsPrincipalAuthenticationContext(
            new ClaimsPrincipal(new ClaimsIdentity(Claims(personId, tenantId, read.IsGlobal, request.RunRoles, permissions), AuthenticationType))));
    }

    /// <summary>
    /// Judges whether a person may still act, now: their account is one a token may be issued to in the tenant, and they hold every permission the run's roles carry (FDW-6137).
    /// The one judgment a run that starts and a schedule's fire (FDW-6144) share, so a person is never judged by two rules.
    /// </summary>
    /// <param name="services">The services: the eligibility check, the tenant provider, the person's permissions and the role grants.</param>
    /// <param name="subject">What the judgment is for: the run, the pipeline and the roles it acts with, as the refusals name them.</param>
    /// <param name="personId">The person.</param>
    /// <param name="tenantId">The tenant they act in.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">Cancels the reads.</param>
    /// <returns>The permissions the roles carry, every one of which the person holds; or the refusal naming why the person may not act.</returns>
    public static async Task<IGenericResult<IReadOnlyCollection<string>>> Judge(
        IServiceProvider services, RunPersonSubject subject, Guid personId, Guid tenantId, ILogger logger, CancellationToken cancellationToken)
    {
        if (services.GetService<IIssuanceEligibility>() is not { } eligibility || services.GetService<ITenantProvider>() is not { } tenants)
        {
            return GenericResult<IReadOnlyCollection<string>>.Failure(
                RunPersonLog.RunPersonCannotBeJudged(logger, subject.ExecutionId, subject.PipelineName, personId));
        }

        var eligible = await Eligible(eligibility, tenants, subject, personId, tenantId, logger, cancellationToken).ConfigureAwait(false);
        return eligible.IsFailure
            ? eligible.ToNewResult<IReadOnlyCollection<string>>()
            : await Carried(services, subject, personId, tenantId, logger, cancellationToken).ConfigureAwait(false);
    }

    // The sign-in check: the account still exists, is active, is not locked out and is a member of the run's tenant. A deny is a refusal naming its reason
    // (RunPersonIneligible); a check that cannot be read is a refusal of its own (RunPersonEligibilityUnreadable).
    private static async Task<IGenericResult> Eligible(
        IIssuanceEligibility eligibility, ITenantProvider tenants, RunPersonSubject subject, Guid personId, Guid tenantId, ILogger logger,
        CancellationToken cancellationToken)
    {
        var decision = await eligibility.MayBeIssued(new Principal { Id = personId, TenantId = tenantId }, tenants, cancellationToken).ConfigureAwait(false);
        if (decision.IsFailure || decision.Value is not { } judged)
        {
            return GenericResult.Failure(RunPersonLog.RunPersonEligibilityUnreadable(
                logger, subject.ExecutionId, subject.PipelineName, personId, tenantId, decision.CurrentMessage));
        }

        return judged.Permitted
            ? GenericResult.Success()
            : GenericResult.Failure(RunPersonLog.RunPersonIneligible(logger, subject.ExecutionId, subject.PipelineName, personId, tenantId, judged.Reason));
    }

    // The permissions the run's roles carry, each of which the person must hold now.
    private static async Task<IGenericResult<IReadOnlyCollection<string>>> Carried(
        IServiceProvider services, RunPersonSubject subject, Guid personId, Guid tenantId, ILogger logger, CancellationToken cancellationToken)
    {
        var held = await services.GetRequiredService<IUserPermissions>()
            .Get(personId.ToString(), tenantId, orgId: null, cancellationToken).ConfigureAwait(false);
        if (held.IsFailure || held.Value is not { } authorization)
        {
            return GenericResult<IReadOnlyCollection<string>>.Failure(RunPersonLog.RunPersonNotPermitted(
                logger, subject.ExecutionId, subject.PipelineName, personId, tenantId, held.CurrentMessage));
        }

        var granted = await services.GetRequiredService<IRoleGrants>().Get(subject.RunRoles, cancellationToken).ConfigureAwait(false);
        if (granted.IsFailure || granted.Value is not { } carried)
        {
            return GenericResult<IReadOnlyCollection<string>>.Failure(
                EtlLog.ExecutionRefusedRunRoles(logger, subject.ExecutionId, subject.PipelineName, granted.CurrentMessage));
        }

        var personPermissions = authorization.Permissions.ToList();
        var notHeld = carried.Where(permission => !PermissionCoverage.Covers(personPermissions, permission)).Distinct(StringComparer.Ordinal).ToList();
        return notHeld.Count == 0
            ? GenericResult<IReadOnlyCollection<string>>.Success(carried)
            : GenericResult<IReadOnlyCollection<string>>.Failure(RunPersonLog.RunPersonRolesNotHeld(
                logger, subject.ExecutionId, subject.PipelineName, personId, string.Join(", ", notHeld)));
    }

    // The claims an acting credential carries (ApiKeyAuthenticationHandler.ResolveActingAs): the person, the tenant, globalTenant for the global tenant,
    // each permission and each role, the role under both `roles` and the standard role claim. The roles are the run's, the permissions what they carry.
    private static List<Claim> Claims(Guid personId, Guid tenantId, bool isGlobal, IReadOnlyList<string> runRoles, IReadOnlyCollection<string> permissions)
    {
        var claims = new List<Claim>
        {
            new(ClaimDefinitions.sub.Name, personId.ToString()),
            new(ClaimDefinitions.tenantId.Name, tenantId.ToString()),
        };
        if (isGlobal)
            claims.Add(new Claim(ClaimDefinitions.globalTenant.Name, "true"));

        claims.AddRange(permissions.Select(permission => new Claim(ClaimDefinitions.perm.Name, permission)));
        claims.AddRange(runRoles.Select(role => new Claim(ClaimDefinitions.roles.Name, role)));
        claims.AddRange(runRoles.Select(role => new Claim(ClaimTypes.Role, role)));
        return claims;
    }
}
