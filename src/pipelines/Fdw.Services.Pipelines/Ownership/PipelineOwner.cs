using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Pipelines.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Pipelines;

/// <summary>
/// Decides which organization a new pipeline belongs to (FDW-4573): the one the request names, otherwise the one the caller acts in, otherwise nothing, which is a refusal and
/// never an unowned pipeline or a made-up organization.
/// </summary>
/// <remarks>
/// The organization is then checked: the caller needs the right to write pipelines at it (one rule: another tenant's organization is not found, no right is forbidden, a
/// platform administrator is admitted); it must belong to the tenant the caller acts in, because the row is stamped with that tenant; and it must be active.
/// </remarks>
public sealed class PipelineOwner
{
    private const string WriteRight = "pipelines:write";
    private const string SourceRequest = "the request";
    private const string SourceCaller = "the organization the caller acts in";

    private readonly IAuthenticationContextAccessor _authentication;
    private readonly IOrganizationScope _scope;
    private readonly IOrganizationProvider _organizations;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineOwner"/> class.</summary>
    /// <param name="authentication">The ambient authentication context: the caller's active tenant and organization.</param>
    /// <param name="scope">Checks the caller's right at an organization and reads its tenant.</param>
    /// <param name="organizations">Reads an organization.</param>
    /// <param name="logger">The logger.</param>
    public PipelineOwner(
        IAuthenticationContextAccessor authentication, IOrganizationScope scope, IOrganizationProvider organizations, ILogger<PipelineOwner>? logger = null)
    {
        _authentication = authentication;
        _scope = scope;
        _organizations = organizations;
        _logger = logger ?? NullLogger<PipelineOwner>.Instance;
    }

    /// <summary>Decides the organization of a new pipeline.</summary>
    /// <param name="requestedOrganizationId">The organization the request names; null when it names none.</param>
    /// <param name="cancellationToken">Cancels the checks.</param>
    /// <returns>The organization's Id; a coded failure when there is none, or the caller may not make a pipeline there.</returns>
    public async Task<IGenericResult<Guid>> OrganizationOfNew(Guid? requestedOrganizationId, CancellationToken cancellationToken = default)
    {
        var caller = _authentication.Current;
        var chosen = Choose(requestedOrganizationId, caller?.ActiveOrgId);
        if (chosen.Organization is not { } organizationId)
        {
            return GenericResult<Guid>.Failure(PipelineOwnerLog.NoOrganization(_logger));
        }

        var tenant = await _scope.TenantOf(WriteRight, organizationId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
        {
            return tenant;
        }

        if (caller?.ActiveTenantId != tenant.Value)
        {
            return GenericResult<Guid>.Failure(PipelineOwnerLog.OrganizationOfAnotherTenant(
                _logger, organizationId, tenant.Value, caller?.ActiveTenantId));
        }

        var organization = await _organizations.Get(organizationId, cancellationToken).ConfigureAwait(false);
        if (organization.IsFailure)
        {
            return organization.ToNewResult<Guid>();
        }

        if (!organization.Value!.IsActive)
        {
            return GenericResult<Guid>.Failure(PipelineOwnerLog.OrganizationNotActive(_logger, organizationId));
        }

        PipelineOwnerLog.OrganizationChosen(_logger, organizationId, chosen.Source);
        return GenericResult<Guid>.Success(organizationId);
    }

    /// <summary>Refuses an update that names an organization other than the one the pipeline already belongs to.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="pipelineName">The pipeline.</param>
    /// <param name="storedOrganizationId">The organization the stored pipeline belongs to; null when it belongs to none.</param>
    /// <param name="requestedOrganizationId">The organization the update names; null when it names none, which keeps the stored one.</param>
    /// <returns>Success; a coded failure when the update names another organization.</returns>
    public static IGenericResult OrganizationUnchanged(ILogger logger, string pipelineName, Guid? storedOrganizationId, Guid? requestedOrganizationId)
    {
        if (requestedOrganizationId is not { } requested || requested == storedOrganizationId)
        {
            return GenericResult.Success();
        }

        return GenericResult.Failure(PipelineOwnerLog.OrganizationCannotChange(
            logger, pipelineName, storedOrganizationId, requested));
    }

    // The request first; the caller's own organization only when the request names none. Two stated sources, in this order, and nothing else.
    private static (Guid? Organization, string Source) Choose(Guid? requested, Guid? acting)
    {
        if (requested is { } named)
        {
            return (named, SourceRequest);
        }

        return acting is { } actingIn ? (actingIn, SourceCaller) : (null, string.Empty);
    }
}
