using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy;

/// <summary>
/// Asks whether the caller may act for the features of an organization (FDW-2474) or of a whole company (FDW-2478).
/// </summary>
/// <remarks>
/// The tenant is the organization's, read here, so a caller cannot name a tenant that is not the organization's. The question is
/// <see cref="IScopeAuthority"/>'s: the caller holds the right at the organization's scope or above. An organization the store does not
/// hold and one the caller may not reach are refused in the same category, so the answer does not say which organizations other tenants hold.
/// A whole company is judged at its own scope, with no organization: the caller holds the right in that company, which they act in. A caller who
/// does not act in it is refused whether or not it exists.
/// </remarks>
internal sealed class FeatureScope
{
    internal const string ReadRight = "organizations:read";
    internal const string WriteRight = "organizations:write";
    internal const string TenantReadRight = "tenants:read";
    internal const string TenantWriteRight = "tenants:write";

    private readonly IOrganizationProvider _organizations;
    private readonly IScopeAuthority _authority;
    private readonly ILogger _logger;

    internal FeatureScope(IOrganizationProvider organizations, IScopeAuthority authority, ILogger logger)
    {
        _organizations = organizations ?? throw new ArgumentNullException(nameof(organizations));
        _authority = authority ?? throw new ArgumentNullException(nameof(authority));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // The tenant of the organization when the caller holds the right for it; otherwise the refusal.
    internal async Task<IGenericResult<Guid>> TenantOf(string right, Guid organizationId, CancellationToken cancellationToken)
    {
        var read = await _organizations.Get(organizationId, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure && !FailureCategory.IsMissing(read))
        {
            return read.ToNewResult<Guid>();
        }

        if (read.IsFailure || read.Value is not { IsDeleted: false } organization)
        {
            return GenericResult<Guid>.Failure(FeatureEnablementLog.OrganizationNotReachable(_logger, right, organizationId));
        }

        var may = await _authority.May(right, new ThingScope(organization.TenantId, organizationId), cancellationToken).ConfigureAwait(false);
        return may.IsFailure
            ? may.ToNewResult<Guid>()
            : GenericResult<Guid>.Success(organization.TenantId);
    }

    // Success when the caller holds the right for the whole company; otherwise the refusal.
    internal Task<IGenericResult> MayActForTenant(string right, Guid tenantId, CancellationToken cancellationToken)
        => _authority.May(right, new ThingScope(tenantId, null), cancellationToken);
}
