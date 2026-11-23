using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Claims.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Configuration;
using Fdw.Services.Multitenancy.Logging;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy;

/// <summary>
/// The one way a list reads the organizations of the caller's tenant (FDW-4828).
/// </summary>
/// <remarks>
/// <para>
/// ConfigurationDb has no row-level policy on the tenant schema, so a read of the organization domain table is not narrowed to the caller's tenant: a list must filter on
/// <c>TenantId</c> itself, and a list that forgot showed a tenant administrator every other tenant's organization names and ids (the tag owners, then the data option owner picker).
/// The filter, the caller's tenant and the read of every organization of one tenant live here and only here, so the organizations route, the tag owners and the owner picker cannot drift.
/// </para>
/// <para>
/// A token with no tenant is refused with its own failure rather than answered with an empty list: "your tenant has no organizations" and "you have no tenant" are different facts. The size
/// of a read of every organization is a setting each caller names, not a value in code; one that is not set, or is below one, fails and names the setting, and a tenant with more
/// organizations than the size fails instead of being listed in part.
/// </para>
/// </remarks>
public static class TenantOrganizations
{
    /// <summary>Gets the tenant the caller's token names, from its <c>tenantId</c> claim.</summary>
    /// <param name="user">The caller.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="operation">What the caller was doing, for the refusal's details.</param>
    /// <returns>The tenant; or the coded failure for a token with none.</returns>
    public static IGenericResult<Guid> CallerTenant(ClaimsPrincipal user, ILogger logger, string operation)
        => Guid.TryParse(user.FindFirst(ClaimDefinitions.tenantId.Name)?.Value, out var tenantId)
            ? GenericResult<Guid>.Success(tenantId)
            : GenericResult.Failure(AuthenticationResultCodes.ByName("CallerHasNoTenant"), logger, ResultDetails.Create("operation", operation)).ToNewResult<Guid>();

    /// <summary>Builds the query for a page of one tenant's organizations: the only filter that narrows the organization domain table to a tenant.</summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="skip">How many of the tenant's organizations precede the page.</param>
    /// <param name="take">How many organizations the page holds at most.</param>
    /// <returns>The query.</returns>
    public static DomainQuery Query(Guid tenantId, int skip, int take)
        => new(
            new FilterExpression
            {
                Root = new FilterCondition
                {
                    PropertyName = nameof(OrganizationDomainConfiguration.TenantId),
                    Operator = FilterOperators.ByName("Equal"),
                    Value = tenantId,
                },
            },
            null, skip, take, false);

    /// <summary>Reads every organization of one tenant, as domain records, in one page.</summary>
    /// <param name="organizations">The organization provider.</param>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="settings">The settings the read size is stated in.</param>
    /// <param name="readSizeSetting">The name of the setting that states how many organizations the read takes; required, with no default.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The organizations; or the failure when the setting is not set or is below one, when the tenant has more organizations than it states, or when the read fails.</returns>
    public static async Task<IGenericResult<IReadOnlyList<OrganizationDomainConfiguration>>> OfTenant(
        this IOrganizationConfigurationProvider organizations,
        Guid tenantId,
        IEffectiveSettingsProvider settings,
        string readSizeSetting,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!settings.TryGetEffectiveValue<int>(readSizeSetting, out var size, tenantId) || size < 1)
            return GenericResult<IReadOnlyList<OrganizationDomainConfiguration>>.Failure(OrganizationLookupLog.ReadSizeNotSet(logger, readSizeSetting));

        var page = await organizations.Get(Query(tenantId, 0, size), cancellationToken).ConfigureAwait(false);
        if (!page.IsSuccess || page.Value is not { } read)
            return page.ToNewResult<IReadOnlyList<OrganizationDomainConfiguration>>();

        var rows = await read.Items.ToListAsync(cancellationToken).ConfigureAwait(false);
        if (read.Outcome.IsFailure)
            return read.Outcome.ToNewResult<IReadOnlyList<OrganizationDomainConfiguration>>();

        return read.HasMore
            ? GenericResult<IReadOnlyList<OrganizationDomainConfiguration>>.Failure(OrganizationLookupLog.ReadSizeTooSmall(logger, readSizeSetting, size))
            : GenericResult<IReadOnlyList<OrganizationDomainConfiguration>>.Success(rows);
    }
}
