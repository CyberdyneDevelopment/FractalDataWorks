using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Settings.Configuration;
using Fdw.Services.Settings.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Base endpoint for listing role-level setting overrides.
/// </summary>
public abstract class ListRoleSettingsEndpointBase : CrudListEndpointBase<RoleSettingSummaryDto>
{
    private readonly IServiceConfigurationProvider<RoleSettingConfiguration> _provider;

    /// <inheritdoc />
    protected ListRoleSettingsEndpointBase(IServiceConfigurationProvider<RoleSettingConfiguration> provider)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "settings/role";

    /// <inheritdoc />
    protected override string EndpointSummary => "List role setting overrides";

    /// <inheritdoc />
    protected override string EndpointDescription => "Returns all role-level setting overrides.";

    /// <inheritdoc />
    protected override async Task<IGenericResult<List<RoleSettingSummaryDto>>> LoadItems(CancellationToken ct)
    {
        SettingsEndpointLog.ListingRoleSettings(Logger, "all", "all");

        var allResult = await _provider.Get(ct).ConfigureAwait(false);
        if (!allResult.IsSuccess)
        {
            return allResult.ToNewResult<List<RoleSettingSummaryDto>>();
        }

        var items = (allResult.Value ?? (IReadOnlyList<RoleSettingConfiguration>)[])
            .Select(s => new RoleSettingSummaryDto
            {
                Id = s.Id,
                TenantId = s.TenantId,
                RoleName = s.RoleName,
                SettingName = s.SettingName,
                SettingValue = s.SettingValue,
                IsActive = s.IsActive
            })
            .ToList();

        return GenericResult<List<RoleSettingSummaryDto>>.Success(items);
    }
}
