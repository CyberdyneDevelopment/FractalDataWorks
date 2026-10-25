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
/// Asks whether the caller may act for a department's retention, and names the tenant the department is in (FDW-2215).
/// </summary>
/// <remarks>
/// The tenant is the department's, read here, so a caller cannot name a tenant that is not the department's. The question is
/// <see cref="IScopeAuthority"/>'s: the caller holds the right at the department's scope or above. A department the store does not hold and
/// one the caller may not reach are refused in the same category, so the answer does not say which departments other tenants hold.
/// </remarks>
internal sealed class RetentionScope
{
    internal const string PolicyReadRight = "retention:read";
    internal const string PolicyWriteRight = "retention:write";
    internal const string HoldReadRight = "legal-holds:read";
    internal const string HoldWriteRight = "legal-holds:write";

    private readonly IOrganizationProvider _organizations;
    private readonly IScopeAuthority _authority;
    private readonly ILogger _logger;

    internal RetentionScope(IOrganizationProvider organizations, IScopeAuthority authority, ILogger logger)
    {
        _organizations = organizations ?? throw new ArgumentNullException(nameof(organizations));
        _authority = authority ?? throw new ArgumentNullException(nameof(authority));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // The tenant of the department when the caller holds the right for it; otherwise the refusal.
    internal async Task<IGenericResult<Guid>> TenantOf(string right, Guid organizationId, CancellationToken cancellationToken)
    {
        var read = await _organizations.Get(organizationId, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure && !FailureCategory.IsMissing(read))
        {
            return read.ToNewResult<Guid>();
        }

        if (read.IsFailure || read.Value is not { IsDeleted: false } organization)
        {
            return GenericResult<Guid>.Failure(RetentionLog.OrganizationNotReachable(_logger, right, organizationId));
        }

        var may = await _authority.May(right, new ThingScope(organization.TenantId, organizationId), cancellationToken).ConfigureAwait(false);
        return may.IsFailure
            ? may.ToNewResult<Guid>()
            : GenericResult<Guid>.Success(organization.TenantId);
    }
}
